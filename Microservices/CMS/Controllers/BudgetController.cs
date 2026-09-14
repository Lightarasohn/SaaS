using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaaS.Extensions;
using SaaS.Microservices.CMS.DTOs.BudgetDTOs;
using SaaS.Microservices.CMS.Interfaces;

namespace SaaS.Microservices.CMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "cost-management")]
    public class BudgetController : ControllerBase
    {
        private readonly IBudgetService _budgetService;
        public BudgetController(IBudgetService budgetService)
        {
            _budgetService = budgetService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? year, [FromQuery] int? month, [FromQuery] Guid? orgUnitPublicId)
        {
            var listResult = await _budgetService.GetAllAsync(year, month, orgUnitPublicId);
            return listResult.ToActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBudgetDTO createBudgetDTO)
        {
            var createdResult = await _budgetService.CreateAsync(createBudgetDTO);
            return createdResult.ToActionResult();
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateBudgetDTO dto)
        {
            var updatedResult = await _budgetService.UpdateBudgetAsync(dto);
            return updatedResult.ToActionResult();
        }

        [HttpPost("can-update")]
        public async Task<IActionResult> CanUpdate([FromBody] CanUpdateBudgetDTO dto)
        {
            var canUpdatedResult = await _budgetService.CanUpdateBudgetAsync(dto);
            return canUpdatedResult.ToActionResult();
        }
    }
}