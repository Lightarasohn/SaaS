using System.Threading.Tasks;
using SaaS.DTOs;
using static SaaS.DTOs.SubscriptionDTOs.SubscriptionDTOs;

namespace SaaS.Interfaces
{
    public interface ISubscriptionService
    {
        Task<Result<SubscriptionDTO>> SubscribeToPlanAsync(Guid companyPublicId, SubscribeToPlanDTO dto);
        Task<Result<SubscriptionDTO>> RenewSubscriptionAsync(Guid companyPublicId, RenewSubscriptionDTO dto);
        Task<Result<SubscriptionDTO>> CancelSubscriptionAsync(Guid companyPublicId);
        Task<Result<SubscriptionDTO>> GetCurrentSubscriptionAsync(Guid companyPublicId);
        Task<Result<List<SubscriptionPlanInfoDTO>>> GetAvailablePlansAsync();
        Task<Result<SubscriptionDTO>> SetAutoRenewAsync(Guid companyPublicId, bool enabled);
    }
}