using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaaS.DTOs;
using SaaS.Microservices.CMS.DTOs.ExpenseDTOs;
using SaaS.Microservices.CMS.Models;

namespace SaaS.Microservices.CMS.Interfaces
{
    public interface IExpenseService
    {
        // [Endpoint] [✔]
        Task<Result<List<ExpenseDTO>>> GetAllAsync(Guid? budgetPublicId, int? statusId, bool onlyMine);
        Task<Result<ExpenseDTO>> GetByIdAsync(Guid expensePublicId);
        // [Endpoint] [✔]
        Task<Result<ExpenseDTO>> CreateAsync(CreateExpenseDTO dto);
        // [Endpoint] [✔]
        Task<Result<List<ExpenseDTO>>> CreateRangeAsync(CreateRangeExpenseDTO dto);
        // [Endpoint] [✔]
        Task<Result> ApproveAsync(ApproveExpenseDTO expensePublicId);
        // [Endpoint] [✔]
        Task<Result<List<Guid>>> ApproveRangeAsync(ApproveRangeExpenseDTO dto);
        // [Endpoint] [✔]
        Task<Result> CanApproveAsync(Guid expensePublicId);
        // [Endpoint] [✔]
        Task<Result<List<Guid>>> CanApproveRangeAsync(CanApproveRangeExpenseDTO dto);
        // [Endpoint] [✔]
        Task<Result> RejectAsync(RejectExpenseDTO dto);
        // [Endpoint] [✔]
        Task<Result<ExpenseDTO>> UpdateExpenseAsync(UpdateExpenseDTO dto);
    }
}