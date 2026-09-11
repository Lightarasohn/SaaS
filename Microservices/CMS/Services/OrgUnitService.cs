using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SaaS.Database.Contexts.CMS;
using SaaS.Database.Contexts.Master;
using SaaS.DTOs;
using SaaS.Interfaces;
using SaaS.Microservices.CMS.DTOs.OrgUnitDTOs;
using SaaS.Microservices.CMS.Interfaces;
using SaaS.Microservices.CMS.Models;
using SaaS.Microservices.CMS.Utils;
using SaaS.Utils;

namespace SaaS.Microservices.CMS.Services
{
    public class OrgUnitService : IOrgUnitService
    {
        private readonly CMSContext _cmsContext;
        private readonly MasterContext _masterContext;
        private readonly ILogger<OrgUnitService> _logger;
        private readonly ICurrentUser _currentUser;

        public OrgUnitService(CMSContext cmsContext, MasterContext masterContext, ILogger<OrgUnitService> logger, ICurrentUser currentUser)
        {
            _cmsContext = cmsContext;
            _masterContext = masterContext;
            _logger = logger;
            _currentUser = currentUser;
        }

        private async Task<Result> AssignRoleInternalAsync(Guid userPublicId, Guid orgUnitPublicId, OrgUnitRoleTypes roleType)
        {
            var companyId = _currentUser.CompanyId;
            var userId = _currentUser.UserId;
            var assignerRole = _currentUser.Role;

            if (companyId == null || userId == null)
                return Result.Unauthorized("Geçersiz Oturum"); // Typo düzeltildi (Geçeresiz -> Geçersiz)

            var user = await _masterContext.AppUsers.FirstOrDefaultAsync(u => u.PublicId == userPublicId);
            if (user == null)
                return Result.NotFound("Kullanıcı bulunamadı");

            var orgUnit = await _cmsContext.OrgUnits.AsNoTracking().FirstOrDefaultAsync(o => o.PublicId == orgUnitPublicId);
            if (orgUnit == null)
                return Result.NotFound("Birim bulunamadı");

            var roleEntity = await _cmsContext.OrgUnitRoles.AsNoTracking().FirstOrDefaultAsync(o => o.Name == roleType.ToString());
            if (roleEntity == null)
                return Result.NotFound("Rol bulunamadı");

            var assignerUserOrgUnitRole = await _cmsContext.OrgUnitUserRoles
                .AsNoTracking()
                .Include(ousr => ousr.OrgUnit)
                .Include(ousr => ousr.Role)
                .FirstOrDefaultAsync(ousr =>
                    ousr.CompanyId == companyId &&
                    ousr.UserId == userId);

            bool isSuperAdmin = assignerRole == RoleTypes.SuperAdmin.ToString();

            // Yetki kontrolü genel mantığı
            bool isHigherManager = assignerUserOrgUnitRole != null
                && assignerUserOrgUnitRole.Role.Name == OrgUnitRoleTypes.Manager.ToString()
                && orgUnit.Path.StartsWith(assignerUserOrgUnitRole.OrgUnit.Path);

            // Manager atamasına özel ekstra iş kuralı: Kendi bulunduğu birime manager atayamaz
            if (roleType == OrgUnitRoleTypes.Manager && isHigherManager)
            {
                if (orgUnit.Path == assignerUserOrgUnitRole!.OrgUnit.Path)
                {
                    isHigherManager = false; // Kendi birimi olduğu için yetkiyi iptal et
                }
            }

            if (isSuperAdmin || isHigherManager)
            {
                user.OrgUnitId = orgUnit.PublicId;
                user.UpdateDate = DateTime.UtcNow;
                user.UpdateUser = userId;
                await _masterContext.SaveChangesAsync();

                var exists = await _cmsContext.OrgUnitUserRoles
                    .FirstOrDefaultAsync(o =>
                        o.CompanyId == companyId &&
                        o.UserId == user.PublicId &&
                        o.IsActive == true);

                if (exists != null)
                {
                    exists.IsActive = false;
                }

                var orgUnitUserRole = new OrgUnitUserRole
                {
                    CompanyId = companyId.Value,
                    OrgUnitId = orgUnit.Id,
                    UserId = user.PublicId,
                    RoleId = roleEntity.Id,
                    CreateDate = DateTime.UtcNow
                };

                await _cmsContext.OrgUnitUserRoles.AddAsync(orgUnitUserRole);
                await _cmsContext.SaveChangesAsync();

                return Result.Success($"Kullanıcı başarıyla birime {roleType} olarak atandı");
            }

            return Result.Forbidden("Bu işlemi yapamazsınız");
        }

        public Task<Result> AssignOrgUnitApproverAsync(AssignOrgUnitDTO dto) =>
    AssignRoleInternalAsync(dto.UserPublicId, dto.OrgUnitPublicId, OrgUnitRoleTypes.Approver);

        public Task<Result> AssignOrgUnitManagerAsync(AssignOrgUnitDTO dto) =>
            AssignRoleInternalAsync(dto.UserPublicId, dto.OrgUnitPublicId, OrgUnitRoleTypes.Manager);

        public Task<Result> AssignOrgUnitUserAsync(AssignOrgUnitDTO dto) =>
            AssignRoleInternalAsync(dto.UserPublicId, dto.OrgUnitPublicId, OrgUnitRoleTypes.User);

        public async Task<Result<OrgUnitDTO>> CreateAsync(CreateOrgUnitDTO dto)
        {
            var companyId = _currentUser.CompanyId;

            if (companyId == null)
                return Result<OrgUnitDTO>.Unauthorized("Geçersiz oturum");

            string parentPath = "/";
            int? parentId = null;

            if (dto.ParentPublicId != Guid.Empty)
            {
                var parent = await _cmsContext.OrgUnits
                    .FirstOrDefaultAsync(d => d.PublicId == dto.ParentPublicId);

                if (parent == null)
                    return Result<OrgUnitDTO>.NotFound("Üst departman bulunamadı");

                parentPath = parent.Path;
                parentId = parent.Id;
            }

            await using var transaction = await _cmsContext.Database.BeginTransactionAsync();
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

                await _cmsContext.OrgUnits.AddAsync(orgUnit);
                await _cmsContext.SaveChangesAsync();

                orgUnit.Path = $"{parentPath}{orgUnit.Id}/";
                await _cmsContext.SaveChangesAsync();

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
            var rows = await _cmsContext.OrgUnits
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