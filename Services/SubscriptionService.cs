using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SaaS.Database.Contexts.Master;
using SaaS.DTOs;
using SaaS.DTOs.ModuleDTOs;
using SaaS.Emails;
using SaaS.Interfaces;
using SaaS.Models;
using SaaS.Utils;
using static SaaS.DTOs.SubscriptionDTOs.SubscriptionDTOs;

namespace SaaS.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly MasterContext _context;
        private readonly IEmailQueue _emailQueue;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(MasterContext context, IEmailQueue emailQueue, ILogger<SubscriptionService> logger)
        {
            _context = context;
            _emailQueue = emailQueue;
            _logger = logger;
        }

        // 1. YENİ PLANA GEÇİŞ VEYA İLK ABONELİK
        public async Task<Result<SubscriptionDTO>> SubscribeToPlanAsync(Guid companyPublicId, SubscribeToPlanDTO dto)
        {
            var company = await _context.Companies
                .Include(c => c.CompanySubscriptions)
                .FirstOrDefaultAsync(c => c.PublicId == companyPublicId);

            if (company == null) return Result<SubscriptionDTO>.Fail("Şirket bulunamadı.");

            var currentSubscription = company.CompanySubscriptions.FirstOrDefault(cs => cs.IsActive);
            var isFree = dto.PlanType == SubscriptionPlanTypes.Free;

            if (currentSubscription != null && currentSubscription.PlanId != (int)SubscriptionPlanTypes.Free && isFree)
            {
                return Result<SubscriptionDTO>.Fail("Ücretli bir plandan doğrudan ücretsiz plana geçemezsiniz. Lütfen mevcut aboneliğinizi iptal edin, dönem sonunda ücretsiz plana otomatik dönülecektir.");
            }

            if (currentSubscription != null && currentSubscription.PlanId == (int)dto.PlanType)
                return Result<SubscriptionDTO>.Fail("Zaten bu plandasınız.");

            if (currentSubscription != null)
                currentSubscription.IsActive = false;

            var expiresAt = isFree
                ? DateTime.UtcNow.AddYears(100)
                : DateTime.UtcNow.AddMonths(1);

            // Free planda yenilenecek bir şey yok
            var autoRenew = !isFree;

            var newSubscription = new CompanySubscription
            {
                CompanyId = company.Id,
                PlanId = (int)dto.PlanType,
                ExpiresAt = expiresAt,
                IsActive = true,
                AutoRenew = autoRenew,
                RenewalFailedAt = null,
                RenewalAttemptCount = 0,
                CreateDate = DateTime.UtcNow
            };

            _context.CompanySubscriptions.Add(newSubscription);
            await _context.SaveChangesAsync();

            var result = await GetCurrentSubscriptionAsync(companyPublicId);

            if (result.IsSuccess)
            {
                await QueueOwnerEmailAsync(company.Id,
                    SubscriptionEmailTemplates.SubscriptionStartedSubject,
                    (r) => SubscriptionEmailTemplates.SubscriptionStartedBody(
                        r.OwnerName, r.CompanyName, result.Data!.SubscriptionPlanName, expiresAt, isFree, autoRenew));
            }

            return result;
        }

        // 2. SÜRE UZATMA — AutoRenew tercihine dokunmaz
        public async Task<Result<SubscriptionDTO>> RenewSubscriptionAsync(Guid companyPublicId, RenewSubscriptionDTO dto)
        {
            if (dto.MonthsToAdd is < 1 or > 24)
                return Result<SubscriptionDTO>.Fail("Eklenecek süre 1 ile 24 ay arasında olmalıdır.");

            var company = await _context.Companies
                .Include(c => c.CompanySubscriptions)
                .FirstOrDefaultAsync(c => c.PublicId == companyPublicId);

            if (company == null)
                return Result<SubscriptionDTO>.Unauthorized("Geçersiz Oturum");

            var currentSubscription = company.CompanySubscriptions.FirstOrDefault(cs => cs.IsActive);

            if (currentSubscription == null || currentSubscription.PlanId == (int)SubscriptionPlanTypes.Free)
                return Result<SubscriptionDTO>.Fail("Yenilenecek aktif bir ücretli abonelik bulunamadı.");

            var baseDate = currentSubscription.ExpiresAt > DateTime.UtcNow
                ? currentSubscription.ExpiresAt
                : DateTime.UtcNow;

            var newExpiresAt = baseDate.AddMonths(dto.MonthsToAdd);

            currentSubscription.IsActive = false;

            var renewedSubscription = new CompanySubscription
            {
                CompanyId = company.Id,
                PlanId = currentSubscription.PlanId,
                ExpiresAt = newExpiresAt,
                IsActive = true,
                // Kullanıcı açıkça belirtmediyse mevcut tercihi korunur.
                // Eski kodda burası her zaman true'ya çekiliyordu; iptal etmiş biri
                // elle süre eklediğinde auto-renew sessizce geri açılıyordu.
                AutoRenew = dto.EnableAutoRenew ?? currentSubscription.AutoRenew,
                // Manuel ödeme geldiği için başarısız tahsilat geçmişi sıfırlanır
                RenewalFailedAt = null,
                RenewalAttemptCount = 0,
                CreateDate = DateTime.UtcNow
            };

            _context.CompanySubscriptions.Add(renewedSubscription);
            await _context.SaveChangesAsync();

            var result = await GetCurrentSubscriptionAsync(companyPublicId);

            if (result.IsSuccess)
            {
                await QueueOwnerEmailAsync(company.Id,
                    SubscriptionEmailTemplates.SubscriptionRenewedSubject,
                    (r) => SubscriptionEmailTemplates.SubscriptionRenewedBody(
                        r.OwnerName, r.CompanyName, result.Data!.SubscriptionPlanName, newExpiresAt, dto.MonthsToAdd));
            }

            return result;
        }

        // 3. İPTAL ETME
        public Task<Result<SubscriptionDTO>> CancelSubscriptionAsync(Guid companyPublicId)
            => SetAutoRenewAsync(companyPublicId, false);

        // 3b. YENİ: Otomatik yenilemeyi aç/kapat (iptali geri almayı da kapsar)
        public async Task<Result<SubscriptionDTO>> SetAutoRenewAsync(Guid companyPublicId, bool enabled)
        {
            var company = await _context.Companies
                .Include(c => c.CompanySubscriptions)
                .FirstOrDefaultAsync(c => c.PublicId == companyPublicId);

            if (company == null)
                return Result<SubscriptionDTO>.Unauthorized("Geçersiz Oturum");

            var currentSubscription = company.CompanySubscriptions.FirstOrDefault(cs => cs.IsActive);

            if (currentSubscription == null || currentSubscription.PlanId == (int)SubscriptionPlanTypes.Free)
                return Result<SubscriptionDTO>.Fail("Otomatik yenileme ayarı yalnızca ücretli abonelikler için geçerlidir.");

            if (currentSubscription.AutoRenew == enabled)
            {
                return Result<SubscriptionDTO>.Fail(enabled
                    ? "Otomatik yenileme zaten açık."
                    : "Abonelik zaten iptal edilmiş (dönem sonunda sonlanacak).");
            }

            if (enabled && currentSubscription.ExpiresAt <= DateTime.UtcNow)
                return Result<SubscriptionDTO>.Fail("Aboneliğinizin süresi dolmuş. Otomatik yenileme yerine yeni bir plan seçmeniz gerekiyor.");

            currentSubscription.IsActive = false;

            var updated = new CompanySubscription
            {
                CompanyId = company.Id,
                PlanId = currentSubscription.PlanId,   // Paket dönem sonuna kadar aynı
                ExpiresAt = currentSubscription.ExpiresAt, // Süre değişmiyor
                IsActive = true,
                AutoRenew = enabled,
                RenewalFailedAt = enabled ? null : currentSubscription.RenewalFailedAt,
                RenewalAttemptCount = enabled ? 0 : currentSubscription.RenewalAttemptCount,
                CreateDate = DateTime.UtcNow
            };

            _context.CompanySubscriptions.Add(updated);
            await _context.SaveChangesAsync();

            var result = await GetCurrentSubscriptionAsync(companyPublicId);

            if (result.IsSuccess)
            {
                var subject = enabled
                    ? SubscriptionEmailTemplates.AutoRenewEnabledSubject
                    : SubscriptionEmailTemplates.SubscriptionCanceledSubject;

                await QueueOwnerEmailAsync(company.Id, subject, (r) => enabled
                    ? SubscriptionEmailTemplates.AutoRenewEnabledBody(r.OwnerName, r.CompanyName, result.Data!.SubscriptionPlanName, updated.ExpiresAt)
                    : SubscriptionEmailTemplates.SubscriptionCanceledBody(r.OwnerName, r.CompanyName, result.Data!.SubscriptionPlanName, updated.ExpiresAt));
            }

            return result;
        }

        // --- Yardımcı: SuperAdmin'e mail kuyruğa atar, hata olsa da işlemi bozmaz ---
        private async Task QueueOwnerEmailAsync(int companyId, string subject, Func<NotificationRecipient, string> bodyFactory)
        {
            try
            {
                var recipient = await _context.GetOwnerRecipientAsync(companyId);

                if (recipient == null || string.IsNullOrWhiteSpace(recipient.Email))
                {
                    _logger.LogWarning("Şirketin SuperAdmin kullanıcısı/e-postası bulunamadı, bildirim gönderilemedi. CompanyId={CompanyId}", companyId);
                    return;
                }

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await _emailQueue.EnqueueAsync(new EmailJob(recipient.Email, subject, bodyFactory(recipient), true), cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Abonelik e-postası kuyruğa alınamadı. CompanyId={CompanyId}, Subject={Subject}", companyId, subject);
            }
        }

        // Yardımcı Metot: Kullanıcının güncel durumunu DTO olarak döner
        // 4. MEVCUT ABONELİĞİ GETİRME
        public async Task<Result<SubscriptionDTO>> GetCurrentSubscriptionAsync(Guid companyPublicId)
        {
            var subscriptionData = await _context.Companies
                .Where(c => c.PublicId == companyPublicId)
                .SelectMany(c => c.CompanySubscriptions.Where(cs => cs.IsActive))
                .Select(cs => new
                {
                    CompanyPublicId = cs.Company.PublicId,
                    CompanyName = cs.Company.Name,
                    PlanId = cs.PlanId,
                    PlanName = cs.Plan.Name,
                    ExpiresAt = cs.ExpiresAt,
                    AutoRenew = cs.AutoRenew,

                    // EF Core çoka-çok ilişkiyi kendi yönettiği için doğrudan Modules'e iniyoruz
                    Modules = cs.Plan.Modules.Select(m => new ModuleDTO(
                        m.ModuleKey,
                        m.Name
                    )).ToList()
                })
                .FirstOrDefaultAsync();

            if (subscriptionData == null)
                return Result<SubscriptionDTO>.Fail("Şirkete ait aktif bir abonelik kaydı bulunamadı.");

            var dto = new SubscriptionDTO(
                CompanyPublicId: subscriptionData.CompanyPublicId,
                SubscriptionPlanId: subscriptionData.PlanId,
                CompanyName: subscriptionData.CompanyName,
                SubscriptionPlanName: subscriptionData.PlanName,
                ExpiresAt: subscriptionData.ExpiresAt,
                AutoRenew: subscriptionData.AutoRenew,
                SubscriptionPlanModules: subscriptionData.Modules
            );

            return Result<SubscriptionDTO>.Success(dto);
        }

        public async Task<Result<List<SubscriptionPlanInfoDTO>>> GetAvailablePlansAsync()
        {
            var plans = await _context.SubscriptionPlans
                .Where(p => p.IsActive)
                .Select(p => new SubscriptionPlanInfoDTO(
                    p.Id,
                    p.Name,
                    p.Price,
                    p.Description,
                    p.Modules.Select(m => new ModuleDTO(m.ModuleKey, m.Name)).ToList()
                ))
                .ToListAsync();

            return Result<List<SubscriptionPlanInfoDTO>>.Success(plans);
        }
    }
}