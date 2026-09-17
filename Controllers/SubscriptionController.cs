using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaaS.DTOs; // Result ve uzantıları için
using SaaS.Extensions;
using SaaS.Interfaces;
using static SaaS.DTOs.SubscriptionDTOs.SubscriptionDTOs;

namespace SaaS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        // Şirketin mevcut abonelik durumunu getirir
        [HttpGet("current")]
        [Authorize] // Sadece yetkili kullanıcılar (Belki buraya Roles = "Admin, SuperAdmin" eklenebilir)
        public async Task<IActionResult> GetCurrentSubscription()
        {
            var companyPublicId = GetCompanyPublicId();
            if (companyPublicId == Guid.Empty)
                return Unauthorized("Geçersiz veya eksik şirket bilgisi.");

            var result = await _subscriptionService.GetCurrentSubscriptionAsync(companyPublicId);

            return result.ToActionResult();
        }

        // Yeni bir plana abone olur / Plan değiştirir
        [HttpPost("subscribe")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> SubscribeToPlan([FromBody] SubscribeToPlanDTO dto)
        {
            var companyPublicId = GetCompanyPublicId();
            if (companyPublicId == Guid.Empty)
                return Unauthorized("Geçersiz veya eksik şirket bilgisi.");

            var result = await _subscriptionService.SubscribeToPlanAsync(companyPublicId, dto);

            return result.ToActionResult();
        }

        // Mevcut aboneliğin süresini uzatır
        [HttpPost("renew")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> RenewSubscription([FromBody] RenewSubscriptionDTO dto)
        {
            var companyPublicId = GetCompanyPublicId();
            if (companyPublicId == Guid.Empty)
                return Unauthorized("Geçersiz veya eksik şirket bilgisi.");

            var result = await _subscriptionService.RenewSubscriptionAsync(companyPublicId, dto);

            return result.ToActionResult();
        }

        // Aboneliği iptal eder (Dönem sonuna kadar aktif kalır)
        [HttpPost("cancel")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CancelSubscription()
        {
            var companyPublicId = GetCompanyPublicId();
            if (companyPublicId == Guid.Empty)
                return Unauthorized("Geçersiz veya eksik şirket bilgisi.");

            var result = await _subscriptionService.CancelSubscriptionAsync(companyPublicId);

            return result.ToActionResult();
        }

        [HttpGet("plans")]
        public async Task<IActionResult> GetAvailablePlans()
        {
            var result = await _subscriptionService.GetAvailablePlansAsync();
            return result.ToActionResult();
        }

        // Otomatik yenilemeyi açar/kapatır (iptali geri almak için de kullanılır)
        [HttpPost("auto-renew")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> SetAutoRenew([FromBody] SetAutoRenewDTO dto)
        {
            var companyPublicId = GetCompanyPublicId();
            if (companyPublicId == Guid.Empty)
                return Unauthorized("Geçersiz veya eksik şirket bilgisi.");

            var result = await _subscriptionService.SetAutoRenewAsync(companyPublicId, dto.Enabled);

            return result.ToActionResult();
        }

        // JWT Token üzerinden company_id claim'ini okuyan yardımcı metot
        private Guid GetCompanyPublicId()
        {
            string companyIdStr = User.FindFirst("company_id")?.Value ?? "";

            if (Guid.TryParse(companyIdStr, out Guid companyPublicId))
            {
                return companyPublicId;
            }

            return Guid.Empty;
        }
    }
}