using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaS.DTOs.ModuleDTOs
{
    public class ModuleAccessDTO
    {
        public string ModuleKey { get; set; } = string.Empty!;
        public string Name { get; set; } = string.Empty!;
        public bool Enabled { get; set; }
    }
}