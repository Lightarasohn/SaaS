using Microsoft.EntityFrameworkCore;
using SaaS.Database.Contexts.Master;
using SaaS.DTOs;
using SaaS.DTOs.ModuleDTOs;
using SaaS.Interfaces;

namespace SaaS.Services;

public class ModuleService : IModuleService
{
    private readonly MasterContext _context;
    private readonly ILogger<ModuleService> _logger;

    public ModuleService(MasterContext context, ILogger<ModuleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<List<ModuleAccessDTO>>> GetModulesAsync(int companyId)
    {
        var allowedIds = await GetAllowedModuleIdsAsync(companyId);

        var modules = await _context.Modules
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.Id)
            .Select(m => new ModuleAccessDTO
            {
                ModuleKey = m.ModuleKey,
                Name = m.Name,
                Enabled = allowedIds.Contains(m.Id)
            })
            .ToListAsync();

        return Result<List<ModuleAccessDTO>>.Success(modules);
    }

    public async Task<bool> HasAccessAsync(int companyId, string moduleKey)
    {
        return await _context.CompanySubscriptions
            .AsNoTracking()
            .Where(cs => cs.CompanyId == companyId
                      && cs.IsActive
                      && cs.ExpiresAt > DateTime.UtcNow)
            .SelectMany(cs => cs.Plan.Modules)
            .AnyAsync(m => m.ModuleKey == moduleKey && m.IsActive);
    }

    private async Task<List<int>> GetAllowedModuleIdsAsync(int companyId)
    {
        return await _context.CompanySubscriptions
            .AsNoTracking()
            .Where(cs => cs.CompanyId == companyId
                      && cs.IsActive
                      && cs.ExpiresAt > DateTime.UtcNow)
            .SelectMany(cs => cs.Plan.Modules)
            .Select(m => m.Id)
            .ToListAsync();
    }
}