using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaaS.Extensions;
using SaaS.Microservices.CMS.DTOs.ExpenseDTOs;
using SaaS.Microservices.CMS.Interfaces;

namespace SaaS.Microservices.CMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "cost-management")]
    public class ExpenseController : ControllerBase
    {
        private readonly IExpenseService _expenseService;

        public ExpenseController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid? budgetPublicId, [FromQuery] int? statusId, [FromQuery] bool onlyMine = false)
        {
            var listResult = await _expenseService.GetAllAsync(budgetPublicId, statusId, onlyMine);
            return listResult.ToActionResult();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var listResult = await _expenseService.GetByIdAsync(id);
            return listResult.ToActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateExpenseDTO createExpenseDTO)
        {
            var createdResult = await _expenseService.CreateAsync(createExpenseDTO);
            return createdResult.ToActionResult();
        }

        [HttpGet("can-approve")]
        public async Task<IActionResult> CanApprove([FromQuery] Guid expensePublicId)
        {
            var canApproveExpenseResult = await _expenseService.CanApproveAsync(expensePublicId);
            return canApproveExpenseResult.ToActionResult();
        }

        [HttpPost("approve")]
        public async Task<IActionResult> Approve([FromBody] ApproveExpenseDTO approveExpenseDTO)
        {
            var approveResult = await _expenseService.ApproveAsync(approveExpenseDTO);
            return approveResult.ToActionResult();
        }

        [HttpPost("reject")]
        public async Task<IActionResult> Reject([FromBody] RejectExpenseDTO rejectExpenseDTO)
        {
            var rejectResult = await _expenseService.RejectAsync(rejectExpenseDTO);
            return rejectResult.ToActionResult();

        }

        [HttpPost("create/range")]
        public async Task<IActionResult> CreateRange([FromBody] CreateRangeExpenseDTO createRangeExpenseDTO)
        {
            var createRangeResult = await _expenseService.CreateRangeAsync(createRangeExpenseDTO);
            return createRangeResult.ToActionResult();
        }

        [HttpPost("approve/range")]
        public async Task<IActionResult> ApproveRange([FromBody] ApproveRangeExpenseDTO approveRangeExpenseDTO)
        {
            var approvedRangeResult = await _expenseService.ApproveRangeAsync(approveRangeExpenseDTO);
            return approvedRangeResult.ToActionResult();
        }

        [HttpPost("can-approve/range")]
        public async Task<IActionResult> CanApproveRange([FromBody] CanApproveRangeExpenseDTO canApproveRangeExpenseDTO)
        {
            var canApprovedRangeResult = await _expenseService.CanApproveRangeAsync(canApproveRangeExpenseDTO);
            return canApprovedRangeResult.ToActionResult();
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateExpenseDTO updateExpenseDTO)
        {
            var updatedResult = await _expenseService.UpdateExpenseAsync(updateExpenseDTO);
            return updatedResult.ToActionResult();
        }
    }
}