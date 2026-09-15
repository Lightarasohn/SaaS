using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaaS.DTOs;
using SaaS.Microservices.CMS.DTOs.ExpenseCategoryDTOs;

namespace SaaS.Microservices.CMS.Interfaces
{
    public interface IExpenseCategoryService
    {
        // [Endpoint] [✔]
        Task<Result<ExpenseCategoryDTO>> CreateAsync(CreateExpenseCategoryDTO dto);
        // [Endpoint] [✔]
        Task<Result<List<ExpenseCategoryDTO>>> GetAllAsync();
        // [Endpoint] [✔]
        Task<Result<ExpenseCategoryDTO>> GetByIdAsync(Guid expenseCategoryPublicId);
        // [Endpoint] [✔]
        Task<Result<ExpenseCategoryDTO>> UpdateAsync(UpdateExpenseCategoryDTO dto);
    }
}