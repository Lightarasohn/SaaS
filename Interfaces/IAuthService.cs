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
        // [Endpoint] [✔]
        public Task<Result<TokenPair>> Login(LoginDTO loginDto, string? ip, string? userAgent);
        // [Endpoint] [✔]
        public Task<Result<string>> RegisterWithCompany(RegisterWithCompanyDTO registerDTO);
        // [Endpoint] [✔]
        public Task<Result<string>> RegisterToCompany(RegisterToCompanyDTO registerDTO);
        // [Endpoint] [✔]
        public Task<Result> VerifyAccount(string rawToken);
        // [Endpoint] [✔]
        public Task<Result> ForgotPassword(ForgotPasswordDTO forgotPasswordDTO);
        // [Endpoint] [✔]
        public Task<Result> ChangePassword(string rawToken, ChangePasswordDTO newPasswordDTO);
        // [Endpoint] [✔]
        public Task<Result> ValidateChangePassword(string rawToken);
        // [Endpoint] [✔]
        public Task<Result> ValidateVerifyAccount(string rawToken);
        // [Endpoint] [✔]
        public Task<Result<TokenPair>> RefreshAsync(string rawRefreshToken, string? ip, string? userAgent);
        // [Endpoint] [✔]
        public Task RevokeRefreshTokenAsync(string rawRefreshToken);
        // [Endpoint] [✔]
        public Task<Result<MeDTO>> GetMe(string publicId);
        // [Endpoint] [✔]
        public Task<Result> ForgotEmail(ForgotEmailDTO forgotEmailDTO);
        // [Endpoint] [✔]
        public Task<Result<ValidateChangeEmailResultDTO>> ValidateChangeEmail(string rawToken);
        // [Endpoint] [✔]
        public Task<Result> ChangeEmail(string rawToken, ChangeEmailDTO newEmailDTO);
        // [Endpoint] [✔]
        public Task<Result> ValidateChangePasswordDirectly(Guid userPublicId);
        // [Endpoint] [✔]
        public Task<Result> ChangePasswordDirectly(Guid userPublicId, ChangePasswordDirectlyDTO changePasswordDirectlyDTO);
    }
}