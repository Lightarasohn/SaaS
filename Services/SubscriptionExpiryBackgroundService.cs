using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaaS.Database.Contexts.Master;
using SaaS.Emails;
using SaaS.Interfaces;
using SaaS.Models;
using SaaS.Utils;
using static SaaS.DTOs.PaymentDTOs.PaymentDTOs;

namespace SaaS.Services
{
    public sealed class SubscriptionExpiryBackgroundService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
        private static readonly TimeSpan GracePeriod = TimeSpan.FromDays(3);
        private const int MaxRenewalAttempts = 3;
        private const int BatchSize = 200;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionExpiryBackgroundService> _logger;

        public SubscriptionExpiryBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<SubscriptionExpiryBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(CheckInterval);

            try
            {
                do
                {
                    try
                    {
                        await ProcessExpiredSubscriptionsAsync(stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        // Bir tur patlasa bile servis ölmesin, sonraki turda tekrar denesin
                        _logger.LogError(ex, "Abonelik süresi kontrolü sırasında beklenmeyen hata.");
                    }
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }

        private async Task ProcessExpiredSubscriptionsAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MasterContext>();
            var emailQueue = scope.ServiceProvider.GetRequiredService<IEmailQueue>();
            var payments = scope.ServiceProvider.GetRequiredService<ISubscriptionPaymentProcessor>();

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;

                var expired = await context.CompanySubscriptions
                    .Include(cs => cs.Plan)
                    .Where(cs => cs.IsActive
                              && cs.PlanId != (int)SubscriptionPlanTypes.Free
                              && cs.ExpiresAt <= now)
                    .OrderBy(cs => cs.Id)
                    .Take(BatchSize)
                    .ToListAsync(stoppingToken);

                if (expired.Count == 0)
                    return;

                var recipients = await context.GetOwnerRecipientsAsync(
                    expired.Select(e => e.CompanyId).Distinct().ToList(), stoppingToken);

                var pendingMails = new List<(int CompanyId, string Subject, string Body)>();

                foreach (var subscription in expired)
                {
                    var planName = subscription.Plan?.Name ?? "Ücretli plan";
                    recipients.TryGetValue(subscription.CompanyId, out var recipient);

                    var ownerName = recipient?.OwnerName ?? "Yetkili";
                    var companyName = recipient?.CompanyName ?? "Hesabınız";

                    // --- AutoRenew KAPALI: doğrudan Free'ye düş ---
                    if (!subscription.AutoRenew)
                    {
                        DowngradeToFree(context, subscription, now);
                        pendingMails.Add((subscription.CompanyId,
                            SubscriptionEmailTemplates.SubscriptionExpiredSubject,
                            SubscriptionEmailTemplates.SubscriptionExpiredBody(ownerName, companyName, planName, subscription.ExpiresAt, false)));
                        continue;
                    }

                    // --- AutoRenew AÇIK: tahsilat dene ---
                    ChargeResult charge;
                    try
                    {
                        charge = await payments.ChargeRenewalAsync(subscription, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Tahsilat çağrısı hata verdi. CompanyId={CompanyId}", subscription.CompanyId);
                        charge = ChargeResult.Fail("Ödeme servisine ulaşılamadı.");
                    }

                    if (charge.Succeeded)
                    {
                        var newExpiresAt = now.AddMonths(1);
                        ReplaceSubscription(context, subscription, s =>
                        {
                            s.PlanId = subscription.PlanId;
                            s.ExpiresAt = newExpiresAt;
                            s.AutoRenew = true;
                            s.RenewalFailedAt = null;
                            s.RenewalAttemptCount = 0;
                        }, now);

                        pendingMails.Add((subscription.CompanyId,
                            SubscriptionEmailTemplates.AutoRenewSucceededSubject,
                            SubscriptionEmailTemplates.AutoRenewSucceededBody(ownerName, companyName, planName, newExpiresAt)));
                        continue;
                    }

                    var attempt = subscription.RenewalAttemptCount + 1;

                    // Deneme hakkı bittiyse veya grace süresi dolduysa Free'ye düş
                    var graceStarted = subscription.RenewalFailedAt ?? now;
                    var graceExhausted = attempt > MaxRenewalAttempts || now - graceStarted >= GracePeriod;

                    if (graceExhausted)
                    {
                        _logger.LogWarning("Yenileme başarısız, Free plana düşürülüyor. CompanyId={CompanyId}, Sebep={Reason}",
                            subscription.CompanyId, charge.FailureReason);

                        DowngradeToFree(context, subscription, now);
                        pendingMails.Add((subscription.CompanyId,
                            SubscriptionEmailTemplates.SubscriptionExpiredSubject,
                            SubscriptionEmailTemplates.SubscriptionExpiredBody(ownerName, companyName, planName, subscription.ExpiresAt, true)));
                        continue;
                    }

                    // Grace period: plan aynı kalır, süre uzatılır, tekrar denenir
                    var graceEndsAt = graceStarted.Add(GracePeriod);

                    ReplaceSubscription(context, subscription, s =>
                    {
                        s.PlanId = subscription.PlanId;
                        s.ExpiresAt = graceEndsAt;
                        s.AutoRenew = true;
                        s.RenewalFailedAt = graceStarted;
                        s.RenewalAttemptCount = attempt;
                    }, now);

                    pendingMails.Add((subscription.CompanyId,
                        SubscriptionEmailTemplates.RenewalFailedSubject,
                        SubscriptionEmailTemplates.RenewalFailedBody(ownerName, companyName, planName, graceEndsAt, attempt, MaxRenewalAttempts)));
                }

                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("{Count} süresi dolmuş abonelik işlendi.", expired.Count);

                // Mailler ancak DB commit edildikten SONRA kuyruğa atılır
                foreach (var mail in pendingMails)
                {
                    if (!recipients.TryGetValue(mail.CompanyId, out var recipient) || string.IsNullOrWhiteSpace(recipient.Email))
                    {
                        _logger.LogWarning("SuperAdmin e-postası yok, bildirim atlandı. CompanyId={CompanyId}", mail.CompanyId);
                        continue;
                    }

                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(10));

                    try
                    {
                        await emailQueue.EnqueueAsync(new EmailJob(recipient.Email, mail.Subject, mail.Body, true), cts.Token);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        // Plan değişikliği zaten kaydedildi; mail hatası turu iptal etmemeli
                        _logger.LogError(ex, "Abonelik bildirimi kuyruğa alınamadı. CompanyId={CompanyId}", mail.CompanyId);
                    }
                }

                if (expired.Count < BatchSize)
                    return;
            }
        }

        private static void DowngradeToFree(MasterContext context, CompanySubscription current, DateTime now)
            => ReplaceSubscription(context, current, s =>
            {
                s.PlanId = (int)SubscriptionPlanTypes.Free;
                s.ExpiresAt = now.AddYears(100);
                s.AutoRenew = false;
                s.RenewalFailedAt = null;
                s.RenewalAttemptCount = 0;
            }, now);

        private static void ReplaceSubscription(
            MasterContext context, CompanySubscription current, Action<CompanySubscription> configure, DateTime now)
        {
            current.IsActive = false;

            var next = new CompanySubscription
            {
                CompanyId = current.CompanyId,
                IsActive = true,
                CreateDate = now
            };

            configure(next);
            context.CompanySubscriptions.Add(next);
        }
    }
}