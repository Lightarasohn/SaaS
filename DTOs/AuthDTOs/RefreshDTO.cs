using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaS.DTOs.AuthDTOs
{
    public class RefreshDTO
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}