using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaaS.DTOs;
using SaaS.DTOs.AuthDTOs;
using SaaS.Utils;

namespace SaaS.Interfaces
{
    public interface IAuthService
    {
        public Task<Result<TokenPair>> Login(LoginDTO loginDto, string? ip, string? userAgent);
        public Task<Result<string>> RegisterWithCompany(RegisterWithCompanyDTO registerDTO);
        public Task<Result<string>> RegisterToCompany(RegisterToCompanyDTO registerDTO);
        public Task<Result<string>> VerifyAccount(string rawToken);
        public Task<Result<string>> ForgotPassword(ForgotPasswordDTO forgotPasswordDTO);
        public Task<Result<string>> ChangePassword(string rawToken, ChangePasswordDTO newPasswordDTO);
        public Task<Result<string>> ValidateChangePassword(string rawToken);
        public Task<Result<TokenPair>> RefreshAsync(string rawRefreshToken, string? ip, string? userAgent);
        public Task RevokeRefreshTokenAsync(string rawRefreshToken);
    }
}