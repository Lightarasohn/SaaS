using Microsoft.AspNetCore.Authorization;
using SaaS.Interfaces;

namespace SaaS.Handlers;

public class ModuleAccessHandler : AuthorizationHandler<ModuleAccessRequirement>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ModuleAccessHandler(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ModuleAccessRequirement requirement)
    {
        var claim = context.User.FindFirst("company_id")?.Value;
        if (!int.TryParse(claim, out int companyId)) return;

        using var scope = _scopeFactory.CreateScope();
        var moduleService = scope.ServiceProvider.GetRequiredService<IModuleService>();

        if (await moduleService.HasAccessAsync(companyId, requirement.ModuleKey))
            context.Succeed(requirement);
    }
}