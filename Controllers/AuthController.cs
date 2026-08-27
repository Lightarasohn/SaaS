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

            SetRefreshCookie(result.Data!.RefreshToken, result.Data.RefreshExpiresAt);

            return Result<string>.Success(result.Data.AccessToken, result.Message).ToActionResult();

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

        [HttpGet]
        [Route("verify-account")]
        public async Task<IActionResult> VerifyAccount([FromQuery] string rawToken)
        {
            var verifyAccountResult = await _authService.VerifyAccount(rawToken);
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

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            string? refreshToken = Request.Cookies["refreshToken"];

            var result = await _authService.RefreshAsync(refreshToken ?? "", GetIp(), GetUserAgent());
            if (!result.IsSuccess)
            {
                Response.Cookies.Delete("refreshToken", RefreshCookieOptions());
                return result.ToActionResult();
            }

            SetRefreshCookie(result.Data!.RefreshToken, result.Data.RefreshExpiresAt);
            return Result<string>.Success(result.Data.AccessToken, result.Message).ToActionResult();

        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            string? refreshToken = Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(refreshToken))
                await _authService.RevokeRefreshTokenAsync(refreshToken);

            Response.Cookies.Delete("refreshToken", RefreshCookieOptions());
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

        private static CookieOptions RefreshCookieOptions() => new CookieOptions
        {
            HttpOnly = true,
            Secure = false,                    // FRONTEND HTTPS DEĞİL!
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth"
        };

        private void SetRefreshCookie(string refreshToken, DateTime expiresAt)
        {
            var options = RefreshCookieOptions();
            options.Expires = expiresAt;
            Response.Cookies.Append("refreshToken", refreshToken, options);
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