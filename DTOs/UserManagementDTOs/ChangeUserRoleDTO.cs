using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaS.DTOs.UserManagementDTOs
{
    public record ChangeUserRoleDTO(Guid UserPublicId, int RoleId);
}