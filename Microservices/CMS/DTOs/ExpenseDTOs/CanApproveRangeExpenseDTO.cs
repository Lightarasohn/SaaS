using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaS.Microservices.CMS.DTOs.ExpenseDTOs
{
    public record CanApproveRangeExpenseDTO(List<Guid> ExpensePublicIdList);
}