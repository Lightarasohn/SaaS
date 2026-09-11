using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaaS.Microservices.CMS.Utils;

namespace SaaS.Microservices.CMS.Interfaces
{
    public interface IOrgUnitAuthorizationService
    {
        Task<bool> IsAuthorizedAsync(Guid userId, int orgUnitId, params OrgUnitRoleTypes[] roles);
    }
}