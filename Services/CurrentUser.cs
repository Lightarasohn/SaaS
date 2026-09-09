using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SaaS.Interfaces;

namespace SaaS.Services;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? User => _accessor.HttpContext?.User;

    public Guid? CompanyId =>
        Guid.TryParse(User?.FindFirst("company_id")?.Value, out Guid id) ? id : null;

    public Guid? UserId =>
        Guid.TryParse(User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out Guid id) ? id : null;

    public string? Role => User?.FindFirst(ClaimTypes.Role)?.Value;

    public string? Name => User?.FindFirst(ClaimTypes.GivenName)?.Value;
}