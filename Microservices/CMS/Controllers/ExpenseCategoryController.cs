using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaaS.Extensions;
using SaaS.Microservices.CMS.DTOs.ExpenseCategoryDTOs;
using SaaS.Microservices.CMS.Interfaces;

namespace SaaS.Microservices.CMS.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "cost-management")]
    public class ExpenseCategoryController : ControllerBase
    {
        private readonly IExpenseCategoryService _expenseCategoryService;

        public ExpenseCategoryController(IExpenseCategoryService expenseCategoryService)
        {
            _expenseCategoryService = expenseCategoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _expenseCategoryService.GetAllAsync();
            return result.ToActionResult();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAll([FromRoute] Guid id)
        {
            var result = await _expenseCategoryService.GetByIdAsync(id);
            return result.ToActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateExpenseCategoryDTO dto)
        {
            var result = await _expenseCategoryService.CreateAsync(dto);
            return result.ToActionResult();
        }

        [HttpPut]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Update([FromBody] UpdateExpenseCategoryDTO dto)
        {
            var result = await _expenseCategoryService.UpdateAsync(dto);
            return result.ToActionResult();
        }
    }
}