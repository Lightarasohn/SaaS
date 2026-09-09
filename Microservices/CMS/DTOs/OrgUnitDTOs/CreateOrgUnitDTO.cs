using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaS.Microservices.CMS.DTOs.OrgUnitDTOs
{
    public record CreateOrgUnitDTO(
        Guid ParentPublicId,
        string Name
    );
}