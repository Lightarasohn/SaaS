using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMS.DTOs;
using CMS.DTOs.AuthDTOs;
using CMS.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CMS.Controllers
{
    [ApiController]
    [Route("api/{controller}")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost]
        [Route("/login")]
        public async Task<IActionResult> Login(LoginDTO loginDTO)
        {
            try
            {
                var loggedInResult = await _authService.Login(loginDTO);
                return Ok(loggedInResult);
            }
            catch
            {
                return BadRequest(Result<string>.Fail());
            }
        }

        [HttpPost]
        [Route("/register/with/company")]
        public async Task<IActionResult> RegisterWithCompany(RegisterWithCompanyDTO registerWithCompanyDTO)
        {
            try
            {
                var registerWithCompanyResult = await _authService.RegisterWithCompany(registerWithCompanyDTO);
                return Ok(registerWithCompanyResult);
            }
            catch (Exception ex)
            {
                return BadRequest(Result<string>.Fail(ex.Message));
            }
        }

        [HttpPost]
        [Route("/register/to/company")]
        public async Task<IActionResult> RegisterToCompany(RegisterToCompanyDTO registerToCompanyDTO)
        {
            try
            {
                var registerToCompanyResult = await _authService.RegisterToCompany(registerToCompanyDTO);
                return Ok(registerToCompanyResult);
            }
            catch
            {
                return BadRequest(Result<string>.Fail());
            }
        }

        [HttpPost]
        [Route("/verify-account")]
        public async Task<IActionResult> VerifyAccount([FromQuery] string publicId)
        {
            try
            {
                var verifyAccountResult = await _authService.VerifyAccount(publicId);
                return Ok(verifyAccountResult);
            }
            catch
            {
                return BadRequest(Result<string>.Fail());
            }
        }
    }
}