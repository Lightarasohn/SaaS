using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaaS.DTOs;
using SaaS.DTOs.ModuleDTOs;
using SaaS.Extensions;
using SaaS.Interfaces;

namespace SaaS.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ModulesController : ControllerBase
{
    private readonly IModuleService _moduleService;

    public ModulesController(IModuleService moduleService)
    {
        _moduleService = moduleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetModules()
    {
        var claim = User.FindFirst("company_id")?.Value;
        if (!Guid.TryParse(claim, out Guid companyId))
            return Result<List<ModuleAccessDTO>>.Unauthorized("Geçersiz oturum").ToActionResult();

        var result = await _moduleService.GetModulesAsync(companyId);
        return result.ToActionResult();
    }
}