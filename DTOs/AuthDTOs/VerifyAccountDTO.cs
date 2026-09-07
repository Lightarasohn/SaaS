using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaS.DTOs.AuthDTOs
{
    public class VerifyAccountDTO
    {
        public string RawToken { get; set; } = string.Empty!;
    }
}