using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaS.Microservices.CMS.DTOs.ExpenseCategoryDTOs
{
    public record ExpenseCategoryDTO(Guid PublicId, string Name, bool IsActive);
}