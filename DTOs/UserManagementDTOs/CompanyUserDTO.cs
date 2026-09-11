using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaS.DTOs.UserManagementDTOs
{
    public record CompanyUserDTO(
        Guid PublicId,
        string Role,
        string Name,
        string Email,
        bool IsVerified,
        DateTime CreateDate
    );
}