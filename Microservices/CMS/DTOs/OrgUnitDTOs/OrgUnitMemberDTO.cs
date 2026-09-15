using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaS.Microservices.CMS.DTOs.OrgUnitDTOs
{
    public record OrgUnitMemberDTO(
        Guid UserPublicId,
        string UserName,
        string Email,
        string RoleName,
        DateTime AssignedAt
    );
}