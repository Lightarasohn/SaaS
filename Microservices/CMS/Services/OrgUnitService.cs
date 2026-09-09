using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SaaS.Database.Contexts.CMS;
using SaaS.DTOs;
using SaaS.Interfaces;
using SaaS.Microservices.CMS.DTOs.OrgUnitDTOs;
using SaaS.Microservices.CMS.Interfaces;
using SaaS.Microservices.CMS.Models;

namespace SaaS.Microservices.CMS.Services
{
    public class OrgUnitService : IOrgUnitService
    {
        private readonly CMSContext _context;
        private readonly ILogger<OrgUnitService> _logger;
        private readonly ICurrentUser _currentUser;

        public OrgUnitService(CMSContext context, ILogger<OrgUnitService> logger, ICurrentUser currentUser)
        {
            _context = context;
            _logger = logger;
            _currentUser = currentUser;
        }

        public async Task<Result<OrgUnitDTO>> CreateAsync(CreateOrgUnitDTO dto)
        {
            var companyId = _currentUser.CompanyId;

            if (companyId == null)
                return Result<OrgUnitDTO>.Unauthorized("Geçersiz oturum");

            string parentPath = "/";
            int? parentId = null;

            if (dto.ParentPublicId != Guid.Empty)
            {
                var parent = await _context.OrgUnits
                    .FirstOrDefaultAsync(d => d.PublicId == dto.ParentPublicId);

                if (parent == null)
                    return Result<OrgUnitDTO>.NotFound("Üst departman bulunamadı");

                parentPath = parent.Path;
                parentId = parent.Id;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var orgUnit = new OrgUnit
                {
                    CompanyId = companyId.Value,
                    ParentId = parentId,
                    Path = "",
                    Name = dto.Name,
                    IsActive = true
                };

                await _context.OrgUnits.AddAsync(orgUnit);
                await _context.SaveChangesAsync();

                orgUnit.Path = $"{parentPath}{orgUnit.Id}/";
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Departman oluşturuldu. Id={Id}, Path={Path}",
                orgUnit.Id, orgUnit.Path);

                return Result<OrgUnitDTO>.Success(new OrgUnitDTO(
                    orgUnit.PublicId,
                    orgUnit.Name,
                    orgUnit.Path.Count(c => c == '/') - 2,
                    orgUnit.IsActive), "Departman oluşturuldu");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Departman oluşturulamadı");
                return Result<OrgUnitDTO>.Fail("Departman oluşturulurken bir hata oluştu", ResultStatus.Error);
            }
        }


        public async Task<Result<List<OrgUnitDTO>>> GetAllAsync()
        {
            var rows = await _context.OrgUnits
                .AsNoTracking()
                .OrderBy(d => d.Path)
                .Select(d => new { d.PublicId, d.Name, d.Path, d.IsActive })
                .ToListAsync();

            var list = rows
                .Select(d => new OrgUnitDTO(
                    d.PublicId,
                    d.Name,
                    d.Path.Count(c => c == '/') - 2,
                    d.IsActive))
                .ToList();

            return Result<List<OrgUnitDTO>>.Success(list);
        }
    }
}