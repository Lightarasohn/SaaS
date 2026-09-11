using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaaS.DTOs;
using SaaS.DTOs.UserManagementDTOs;

namespace SaaS.Interfaces
{
    public interface IUserManagementService
    {
        // [Endpoint] [✔]
        Task<Result<List<CompanyUserDTO>>> GetCompanyUsersAsync();
        // [Endpoint] [✔]
        Task<Result> ChangeRoleAsync(ChangeUserRoleDTO dto);
    }
}