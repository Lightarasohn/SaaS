using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaS.Microservices.CMS.DTOs.BudgetDTOs
{
    public record CanUpdateBudgetDTO(Guid BudgetPublicId, Guid OrgUnitPublicId, int Year, int Month, decimal TotalAmount);
}