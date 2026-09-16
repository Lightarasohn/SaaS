using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaS.DTOs.AuthDTOs
{
    public record ChangePasswordDirectlyDTO(string OldPassword, string NewPassword);
}