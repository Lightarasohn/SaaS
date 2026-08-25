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
            try
            {
                var result = await _authService.Login(loginDTO, GetIp(), GetUserAgent());
                if (!result.IsSuccess) return BadRequest(result);

                SetRefreshCookie(result.Data!.RefreshToken, result.Data.RefreshExpiresAt);

                return Ok(Result<string>.Success(result.Data.AccessToken, result.Message));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL ON \"auth/login\": {ex.Message}");
                return BadRequest(Result<string>.Fail());
            }
        }

        [HttpPost]
        [Route("register/with/company")]
        public async Task<IActionResult> RegisterWithCompany(RegisterWithCompanyDTO registerWithCompanyDTO)
        {
            try
            {
                var registerWithCompanyResult = await _authService.RegisterWithCompany(registerWithCompanyDTO);
                return Ok(registerWithCompanyResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL ON \"auth/register/with/company\": {ex.Message}");
                return BadRequest(Result<string>.Fail());
            }
        }

        [HttpPost]
        [Route("register/to/company")]
        public async Task<IActionResult> RegisterToCompany(RegisterToCompanyDTO registerToCompanyDTO)
        {
            try
            {
                var registerToCompanyResult = await _authService.RegisterToCompany(registerToCompanyDTO);
                return Ok(registerToCompanyResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL ON \"auth/register/to/company\": {ex.Message}");
                return BadRequest(Result<string>.Fail());
            }
        }

        [HttpGet]
        [Route("verify-account")]
        public async Task<IActionResult> VerifyAccount([FromQuery] string rawToken)
        {
            try
            {
                var verifyAccountResult = await _authService.VerifyAccount(rawToken);
                return Ok(verifyAccountResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL ON \"auth/verify-account\": {ex.Message}");
                return BadRequest(Result<string>.Fail());
            }
        }

        [HttpPost]
        [Route("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO forgotPasswordDTO)
        {
            try
            {
                var forgotPasswordResult = await _authService.ForgotPassword(forgotPasswordDTO);
                return Ok(forgotPasswordResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL ON \"auth/forgot-password\": {ex.Message}");
                return BadRequest(Result<string>.Fail());
            }
        }

        [HttpPost]
        [Route("change-password")]
        public async Task<IActionResult> ChangePassword([FromQuery] string token, [FromBody] ChangePasswordDTO changePasswordDTO)
        {
            try
            {
                var changePasswordResult = await _authService.ChangePassword(token, changePasswordDTO);
                return Ok(changePasswordResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL ON \"auth/change-password\": {ex.Message}");
                return BadRequest(Result<string>.Fail());
            }
        }

        [HttpGet]
        [Route("validate-change-password")]
        public async Task<IActionResult> ValidateChangePassword([FromQuery] string token)
        {
            try
            {
                var validateChangePasswordResult = await _authService.ValidateChangePassword(token);
                return Ok(validateChangePasswordResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL ON \"auth/validate-change-password\": {ex.Message}");
                return BadRequest(Result<string>.Fail());
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            try
            {
                string? refreshToken = Request.Cookies["refreshToken"];

                var result = await _authService.RefreshAsync(refreshToken ?? "", GetIp(), GetUserAgent());
                if (!result.IsSuccess)
                {
                    Response.Cookies.Delete("refreshToken");
                    return Unauthorized(result);
                }

                SetRefreshCookie(result.Data!.RefreshToken, result.Data.RefreshExpiresAt);
                return Ok(Result<string>.Success(result.Data.AccessToken, result.Message));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL ON \"auth/refresh\": {ex.Message}");
                return BadRequest(Result<string>.Fail());
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                string? refreshToken = Request.Cookies["refreshToken"];
                if (!string.IsNullOrEmpty(refreshToken))
                    await _authService.RevokeRefreshTokenAsync(refreshToken);

                Response.Cookies.Delete("refreshToken");
                return Ok(Result<string>.Success("Çıkış yapıldı"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL ON \"auth/logout\": {ex.Message}");
                return BadRequest(Result<string>.Fail());
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            try
            {
                string publicId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "";

                var getMeResult = await _authService.GetMe(publicId);

                return Ok(getMeResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL ON \"auth/me\": {ex.Message}");
                return BadRequest(Result<string>.Fail());
            }
        }

        private void SetRefreshCookie(string refreshToken, DateTime expiresAt)
        {
            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,                      // FRONTEND HTTPS DEĞİL!
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt,
                Path = "/api/auth"
            });
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