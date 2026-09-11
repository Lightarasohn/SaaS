using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaS.Microservices.CMS.DTOs.ExpenseCategoryDTOs
{
    public record UpdateExpenseCategoryDTO(Guid ExpenseCategoryPublicId, string Name);
}