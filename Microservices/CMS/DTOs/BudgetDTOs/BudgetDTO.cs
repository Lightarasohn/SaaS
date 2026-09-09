using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaS.Microservices.CMS.DTOs.BudgetDTOs
{
    public record BudgetDTO(
        Guid PublicId,
        Guid OrgUnitPublicId,
        string OrgUnitName,
        int Month,
        int Year,
        decimal TotalAmount,
        decimal UsedAmount,
        decimal RemainingAmount
    );
}