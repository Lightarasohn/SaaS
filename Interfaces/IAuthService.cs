using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaaS.DTOs;
using SaaS.DTOs.AuthDTOs;
using SaaS.Models;

namespace SaaS.Interfaces
{
    public interface IAuthService
    {
        public Task<Result<AppUser>> Login(LoginDTO loginDto);
        public Task<Result<string>> RegisterWithCompany(RegisterWithCompanyDTO registerDTO);
        public Task<Result<string>> RegisterToCompany(RegisterToCompanyDTO registerDTO);
        public Task<Result<string>> VerifyAccount(string userPublicId);
    }
}