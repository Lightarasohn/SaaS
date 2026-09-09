using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaaS.DTOs;
using SaaS.Microservices.CMS.DTOs.BudgetDTOs;

namespace SaaS.Microservices.CMS.Interfaces
{
    public interface IBudgetService
    {
        Task<Result<List<BudgetDTO>>> GetAllAsync(int? year, int? month, Guid? orgUnitPublicId);
        Task<Result<BudgetDTO>> CreateAsync(CreateBudgetDTO dto);
    }
}