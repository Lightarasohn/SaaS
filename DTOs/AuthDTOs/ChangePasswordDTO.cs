using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaS.DTOs.AuthDTOs
{
    public class ChangePasswordDTO
    {
        public string NewPassword { get; set; } = string.Empty!;
    }
}