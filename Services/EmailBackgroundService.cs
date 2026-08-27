using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaaS.Emails;
using SaaS.Interfaces;

namespace SaaS.Services
{
    public sealed class EmailBackgroundService : BackgroundService
    {
        private readonly IEmailQueue _emailQueue;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<EmailBackgroundService> _logger;
        public EmailBackgroundService(IEmailQueue emailQueue, IServiceScopeFactory serviceScopeFactory, ILogger<EmailBackgroundService> logger)
        {
            _emailQueue = emailQueue;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var emailJob in _emailQueue.ReadAllAsync(stoppingToken))
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    for (int attempt = 1; attempt <= 3; attempt++)
                    {
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                        cts.CancelAfter(TimeSpan.FromSeconds(30));

                        try
                        {
                            await emailService.SendEmailAsync(emailJob.ToEmail, emailJob.Subject, emailJob.Message, cts.Token, emailJob.IsHtml);
                            break;
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) // uygulama kapanma request'i
                        {
                            return;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"SMTP/Email Hatası (deneme {attempt}/3): {ex.Message}");
                            if (attempt < 3)
                                await Task.Delay(TimeSpan.FromSeconds(2 * attempt), stoppingToken);
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) // uygulama kapanma request'i
            {
                return;
            }
        }
    }
}