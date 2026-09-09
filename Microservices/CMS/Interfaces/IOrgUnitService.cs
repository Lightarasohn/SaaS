using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaaS.DTOs;
using SaaS.Microservices.CMS.DTOs.OrgUnitDTOs;

namespace SaaS.Microservices.CMS.Interfaces
{
    public interface IOrgUnitService
    {
        Task<Result<List<OrgUnitDTO>>> GetAllAsync();
        Task<Result<OrgUnitDTO>> CreateAsync(CreateOrgUnitDTO dto);
    }
}