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

        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Create([FromBody] CreateOrgUnitDTO dto)
        {
            var result = await _orgUnitService.CreateAsync(dto);
            return result.ToActionResult();
        }

        [HttpPost]
        [Route("assign/user")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> AssignUser(AssignOrgUnitDTO assignOrgUnitDTO)
        {
            var result = await _orgUnitService.AssignOrgUnitUserAsync(assignOrgUnitDTO);
            return result.ToActionResult();
        }

        [HttpPost]
        [Route("assign/approver")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> AssignApprover(AssignOrgUnitDTO assignOrgUnitDTO)
        {
            var result = await _orgUnitService.AssignOrgUnitApproverAsync(assignOrgUnitDTO);
            return result.ToActionResult();
        }

        [HttpPost]
        [Route("assign/manager")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> AssignManager(AssignOrgUnitDTO assignOrgUnitDTO)
        {
            var result = await _orgUnitService.AssignOrgUnitManagerAsync(assignOrgUnitDTO);
            return result.ToActionResult();
        }
    }
}