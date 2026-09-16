using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaaS.Extensions;
using SaaS.Microservices.CMS.DTOs.OrgUnitDTOs;
using SaaS.Microservices.CMS.Interfaces;

namespace SaaS.Microservices.CMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "cost-management")]
    public class OrgUnitController : ControllerBase
    {
        private readonly IOrgUnitService _orgUnitService;

        public OrgUnitController(IOrgUnitService orgUnitService)
        {
            _orgUnitService = orgUnitService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var listResult = await _orgUnitService.GetAllAsync();
            return listResult.ToActionResult();
        }

        [HttpGet("{orgUnitPublicId:guid}/members")]
        public async Task<IActionResult> GetMembers(Guid orgUnitPublicId)
        {
            var result = await _orgUnitService.GetMembersAsync(orgUnitPublicId);
            return result.ToActionResult();
        }

        [HttpPut]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Update([FromBody] UpdateOrgUnitDTO dto)
        {
            var result = await _orgUnitService.UpdateAsync(dto);
            return result.ToActionResult();
        }

        [HttpPost("toggle-active")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ToggleActive([FromBody] ToggleOrgUnitDTO dto)
        {
            var result = await _orgUnitService.ToggleActiveAsync(dto);
            return result.ToActionResult();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var result = await _orgUnitService.GetById(id);
            return result.ToActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrgUnitDTO dto)
        {
            var result = await _orgUnitService.CreateAsync(dto);
            return result.ToActionResult();
        }

        [HttpDelete("{orgUnitPublicId:guid}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(Guid orgUnitPublicId)
        {
            var result = await _orgUnitService.DeleteAsync(orgUnitPublicId);
            return result.ToActionResult();
        }

        [HttpPost]
        [Route("assign/user")]
        public async Task<IActionResult> AssignUser(AssignOrgUnitDTO assignOrgUnitDTO)
        {
            var result = await _orgUnitService.AssignOrgUnitUserAsync(assignOrgUnitDTO);
            return result.ToActionResult();
        }

        [HttpPost]
        [Route("assign/approver")]
        public async Task<IActionResult> AssignApprover(AssignOrgUnitDTO assignOrgUnitDTO)
        {
            var result = await _orgUnitService.AssignOrgUnitApproverAsync(assignOrgUnitDTO);
            return result.ToActionResult();
        }

        [HttpPost]
        [Route("assign/manager")]
        public async Task<IActionResult> AssignManager(AssignOrgUnitDTO assignOrgUnitDTO)
        {
            var result = await _orgUnitService.AssignOrgUnitManagerAsync(assignOrgUnitDTO);
            return result.ToActionResult();
        }
    }
}