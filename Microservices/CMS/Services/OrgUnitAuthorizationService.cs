using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SaaS.Database.Contexts.CMS;
using SaaS.Microservices.CMS.Interfaces;
using SaaS.Microservices.CMS.Utils;

namespace SaaS.Microservices.CMS.Services
{
    public class OrgUnitAuthorizationService : IOrgUnitAuthorizationService
    {
        private readonly CMSContext _context;
        public OrgUnitAuthorizationService(CMSContext context) => _context = context;

        public async Task<bool> IsAuthorizedAsync(Guid userId, int orgUnitId, params OrgUnitRoleTypes[] roles)
        {
            var target = await _context.OrgUnits.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orgUnitId);

            if (target == null) return false;

            var roleNames = roles.Select(r => r.ToString()).ToList();

            // Kullanıcının rolü olduğu birimler, hedef birimin üst kademesi (veya kendisi) mi?
            var candidatePaths = await _context.OrgUnitUserRoles
                .AsNoTracking()
                .Where(r => r.UserId == userId && r.IsActive && roleNames.Contains(r.Role.Name))
                .Join(_context.OrgUnits, r => r.OrgUnitId, o => o.Id, (r, o) => o.Path)
                .ToListAsync();

            return candidatePaths.Any(p => target.Path.StartsWith(p));
        }

        public async Task<List<string>> GetAuthorizedOrgUnitPathsAsync(Guid userId, params OrgUnitRoleTypes[] roles)
        {
            var roleNames = roles.Select(r => r.ToString()).ToList();

            return await _context.OrgUnitUserRoles
                .AsNoTracking()
                .Where(r => r.UserId == userId && r.IsActive && roleNames.Contains(r.Role.Name))
                .Join(_context.OrgUnits, r => r.OrgUnitId, o => o.Id, (r, o) => o.Path)
                .ToListAsync();
        }
    }
}