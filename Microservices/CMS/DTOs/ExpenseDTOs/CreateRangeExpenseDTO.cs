using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaS.Microservices.CMS.DTOs.ExpenseDTOs
{
    public record ExpenseItemDTO(
        Guid ExpenseCategoryPublicId,
        decimal Amount,
        string? Description
    );

    public record CreateRangeExpenseDTO(
        Guid BudgetPublicId,
        List<ExpenseItemDTO> Items
    );
}