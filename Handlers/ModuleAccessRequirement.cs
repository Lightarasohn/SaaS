using Microsoft.AspNetCore.Authorization;

namespace SaaS.Handlers;

public class ModuleAccessRequirement : IAuthorizationRequirement
{
    public string ModuleKey { get; }

    public ModuleAccessRequirement(string moduleKey) => ModuleKey = moduleKey;
}