using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaaS.DTOs.UserManagementDTOs;
using SaaS.Extensions;
using SaaS.Interfaces;

namespace SaaS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class UserManagemetController : ControllerBase
    {
        private readonly IUserManagementService _userManagementService;

        public UserManagemetController(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCompanyUsers()
        {
            var usersResult = await _userManagementService.GetCompanyUsersAsync();
            return usersResult.ToActionResult();
        }

        [HttpPut]
        public async Task<IActionResult> ChangeRole(ChangeUserRoleDTO changeUserRoleDTO)
        {
            var result = await _userManagementService.ChangeRoleAsync(changeUserRoleDTO);
            return result.ToActionResult();
        }
    }
}