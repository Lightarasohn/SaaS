using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaaS.Interfaces;
using SaaS.Models;
using static SaaS.DTOs.PaymentDTOs.PaymentDTOs;

namespace SaaS.Services
{
    /// <summary>
    /// Ödeme sağlayıcısı entegre edilene kadar kullanılan geçici implementasyon.
    /// Tahsilat yapmadan başarılı döner — yani auto-renew açık olan herkesin süresi ücretsiz uzar.
    /// Prod'a çıkmadan önce gerçek bir processor ile değiştirilmeli.
    /// </summary>
    public sealed class AlwaysSucceedPaymentProcessor : ISubscriptionPaymentProcessor
    {
        private readonly ILogger<AlwaysSucceedPaymentProcessor> _logger;

        public AlwaysSucceedPaymentProcessor(ILogger<AlwaysSucceedPaymentProcessor> logger)
        { 
            _logger = logger;
        }

        public Task<ChargeResult> ChargeRenewalAsync(CompanySubscription subscription, CancellationToken ct)
        {
            _logger.LogWarning(
                "Ödeme entegrasyonu yok, tahsilat atlandı. CompanyId={CompanyId}, PlanId={PlanId}",
                subscription.CompanyId, subscription.PlanId);

            return Task.FromResult(ChargeResult.Success());
        }
    }
}