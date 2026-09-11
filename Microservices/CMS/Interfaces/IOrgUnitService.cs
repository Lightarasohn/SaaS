using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaaS.DTOs;
using SaaS.Microservices.CMS.DTOs.OrgUnitDTOs;
using SaaS.Microservices.CMS.Utils;

namespace SaaS.Microservices.CMS.Interfaces
{
    public interface IOrgUnitService
    {
        // [Endpoint] [✔]
        Task<Result<List<OrgUnitDTO>>> GetAllAsync();
        // [Endpoint] [✔]
        Task<Result<OrgUnitDTO>> CreateAsync(CreateOrgUnitDTO dto);
        // [Endpoint] [✔]
        Task<Result> AssignOrgUnitUserAsync(AssignOrgUnitDTO dto);
        // [Endpoint] [✔]
        Task<Result> AssignOrgUnitManagerAsync(AssignOrgUnitDTO dto);
        // [Endpoint] [✔]
        Task<Result> AssignOrgUnitApproverAsync(AssignOrgUnitDTO dto);
    }
}