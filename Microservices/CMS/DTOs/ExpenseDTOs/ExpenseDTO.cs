using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaS.Microservices.CMS.DTOs.ExpenseDTOs
{
    public record ExpenseDTO(
        Guid PublicId,
        Guid UserPublicId,
        Guid BudgetPublicId,
        Guid ExpenseCategoryPublicId,
        int StatusId,
        string ExpenseCategoryName,
        decimal Amount,
        string? Description,
        string? RejectReason,
        int BudgetMonth,
        int BudgetYear,
        string Status,
        string CreateUserName,
        DateTime CreateDate,
        string OrgUnitName,
        bool OrgUnitIsActive
    );
}