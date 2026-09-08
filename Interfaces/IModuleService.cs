using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaaS.DTOs;
using SaaS.DTOs.ModuleDTOs;

namespace SaaS.Interfaces
{
    public interface IModuleService
    {
        Task<Result<List<ModuleAccessDTO>>> GetModulesAsync(int companyId);
        Task<bool> HasAccessAsync(int companyId, string moduleKey);
    }
}