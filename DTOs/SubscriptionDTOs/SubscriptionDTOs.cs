using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaaS.DTOs.ModuleDTOs;
using SaaS.Utils;

namespace SaaS.DTOs.SubscriptionDTOs
{
    public static class SubscriptionDTOs
    {
        public record SubscribeToPlanDTO(SubscriptionPlanTypes PlanType);
        public record RenewSubscriptionDTO(int MonthsToAdd, bool? EnableAutoRenew = null);

        public record SetAutoRenewDTO(bool Enabled);

        public record SubscriptionDTO(
            Guid CompanyPublicId, 
            int SubscriptionPlanId, 
            string CompanyName, 
            string SubscriptionPlanName, 
            DateTime ExpiresAt,
            bool AutoRenew,
            List<ModuleDTO> SubscriptionPlanModules);
    
        public record SubscriptionPlanInfoDTO(
            int Id, 
            string Name, 
            decimal Price, 
            string? Description, 
            List<ModuleDTO> Modules
        );
    }
}
