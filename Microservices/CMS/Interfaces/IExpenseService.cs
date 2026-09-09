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
        Task<Result<List<ExpenseDTO>>> GetAllAsync(Guid? budgetPublicId, int? statusId, bool onlyMine);
        Task<Result<ExpenseDTO>> CreateAsync(CreateExpenseDTO dto);
        Task<Result> ApproveAsync(Guid expensePublicId);
        Task<Result> CanApproveAsync(Guid expensePublicId);
        Task<Result> RejectAsync(Guid expensePublicId, RejectExpenseDTO dTO);
    }
}