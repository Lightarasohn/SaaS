using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaS.Microservices.CMS.DTOs.ExpenseDTOs
{
    public record RejectExpenseDTO(string? RejectReason);
}