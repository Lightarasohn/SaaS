using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.DTOs.AuthDTOs
{
    public class RegisterWithCompanyDTO
    {
        public string CompanyName {get; set;} = null!;

        public string Name { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;
    }
}