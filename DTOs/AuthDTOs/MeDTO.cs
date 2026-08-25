using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaaS.Models;

namespace SaaS.DTOs.AuthDTOs
{
    public class MeDTO
    {
        public Guid PublicId { get; set; }

        public string Name { get; set; } = null!;

        public string Email { get; set; } = null!;

        public bool IsVerified { get; set; }

        public DateTime? PasswordChangedAt { get; set; }

        public DateTime CreateDate { get; set; }

        public string CompanyName { get; set; } = null!;

        public string RoleName { get; set; } = null!;
    }
}