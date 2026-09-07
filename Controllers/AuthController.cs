using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaaS.DTOs;
using SaaS.DTOs.AuthDTOs;
using SaaS.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using SaaS.Extensions;
using SaaS.Utils;

namespace SaaS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDTO loginDTO)
        {  
            var result = await _authService.Login(loginDTO, GetIp(), GetUserAgent());
            if (!result.IsSuccess) return result.ToActionResult();

            var tokens = new TokenPair(
                AccessToken: result.Data!.AccessToken,
                AccessTokenExpiresAt: result.Data.AccessTokenExpiresAt,
                RefreshToken: result.Data.RefreshToken,
                RefreshTokenExpiresAt: result.Data.RefreshTokenExpiresAt
            );

            return Result<TokenPair>.Success(tokens, result.Message).ToActionResult();

        }

        [HttpPost]
        [Route("register/with/company")]
        public async Task<IActionResult> RegisterWithCompany(RegisterWithCompanyDTO registerWithCompanyDTO)
        {
            var registerWithCompanyResult = await _authService.RegisterWithCompany(registerWithCompanyDTO);
            return registerWithCompanyResult.ToActionResult();

        }

        [HttpPost]
        [Route("register/to/company")]
        public async Task<IActionResult> RegisterToCompany(RegisterToCompanyDTO registerToCompanyDTO)
        {
            var registerToCompanyResult = await _authService.RegisterToCompany(registerToCompanyDTO);
            return registerToCompanyResult.ToActionResult();

        }

        [HttpPost]
        [Route("verify-account")]
        public async Task<IActionResult> VerifyAccount([FromBody] VerifyAccountDTO verifyAccountDTO)
        {
            var verifyAccountResult = await _authService.VerifyAccount(verifyAccountDTO.RawToken);
            return verifyAccountResult.ToActionResult();

        }

        [HttpPost]
        [Route("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO forgotPasswordDTO)
        {
            var forgotPasswordResult = await _authService.ForgotPassword(forgotPasswordDTO);
            return forgotPasswordResult.ToActionResult();

        }

        [HttpPost]
        [Route("change-password")]
        public async Task<IActionResult> ChangePassword([FromQuery] string token, [FromBody] ChangePasswordDTO changePasswordDTO)
        {
            var changePasswordResult = await _authService.ChangePassword(token, changePasswordDTO);
            return changePasswordResult.ToActionResult();

        }

        [HttpGet]
        [Route("validate-change-password")]
        public async Task<IActionResult> ValidateChangePassword([FromQuery] string token)
        {
            var validateChangePasswordResult = await _authService.ValidateChangePassword(token);
            return validateChangePasswordResult.ToActionResult();

        }

        [HttpGet]
        [Route("validate-verify-account")]
        public async Task<IActionResult> ValidateVerifyAccount([FromQuery] string rawToken)
        {
            var validateVerifyAccountResult = await _authService.ValidateVerifyAccount(rawToken);
            return validateVerifyAccountResult.ToActionResult();
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshDTO refreshDTO)
        {

            var result = await _authService.RefreshAsync(refreshDTO.RefreshToken ?? "", GetIp(), GetUserAgent());
            if (!result.IsSuccess)
            {
                return result.ToActionResult();
            }

            var tokens = new TokenPair(
                AccessToken: result.Data!.AccessToken,
                AccessTokenExpiresAt: result.Data.AccessTokenExpiresAt,
                RefreshToken: result.Data.RefreshToken,
                RefreshTokenExpiresAt: result.Data.RefreshTokenExpiresAt
            );

            return Result<TokenPair>.Success(tokens, result.Message).ToActionResult();

        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshDTO refreshDTO)
        {
            if (!string.IsNullOrEmpty(refreshDTO.RefreshToken))
                await _authService.RevokeRefreshTokenAsync(refreshDTO.RefreshToken);

            return Result.Success("Çıkış Yapıldı").ToActionResult();

        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            string publicId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "";

            var getMeResult = await _authService.GetMe(publicId);

            return getMeResult.ToActionResult();

        }

        private string? GetIp()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }
        private string? GetUserAgent()
        {
            return Request.Headers.UserAgent.ToString();
        }
    }
}