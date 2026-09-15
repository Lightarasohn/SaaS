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
        private readonly IOrgUnitAuthorizationService _orgUnitAuthService;
        private readonly ILogger<OrgUnitService> _logger;
        private readonly ICurrentUser _currentUser;

        public OrgUnitService(CMSContext cmsContext, MasterContext masterContext, IOrgUnitAuthorizationService orgUnitAuthService, ILogger<OrgUnitService> logger, ICurrentUser currentUser)
        {
            _cmsContext = cmsContext;
            _masterContext = masterContext;
            _orgUnitAuthService = orgUnitAuthService;
            _logger = logger;
            _currentUser = currentUser;
        }

        private async Task<Result> AssignRoleInternalAsync(Guid userPublicId, Guid orgUnitPublicId, OrgUnitRoleTypes roleType)
        {
            var companyId = _currentUser.CompanyId;
            var userId = _currentUser.UserId;
            var assignerRole = _currentUser.Role;

            if (companyId == null || userId == null)
                return Result.Unauthorized("Geçersiz Oturum");

            var company = await _masterContext.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.PublicId == companyId);

            if (company == null)
                return Result.NotFound("Organizasyon bulunamadı");

            var user = await _masterContext.AppUsers.FirstOrDefaultAsync(u => u.PublicId == userPublicId);
            if (user == null)
                return Result.NotFound("Kullanıcı bulunamadı");

            // YENİ: kullanıcı gerçekten bu şirkete mi ait — cross-tenant atamayı engeller
            if (user.CompanyId != company.Id)
                return Result.Forbidden("Bu işlemi yapamazsınız");

            var orgUnit = await _cmsContext.OrgUnits.AsNoTracking().FirstOrDefaultAsync(o => o.PublicId == orgUnitPublicId);
            if (orgUnit == null)
                return Result.NotFound("Birim bulunamadı");

            // YENİ: pasif birime rol atanamaz
            if (!orgUnit.IsActive)
                return Result.Fail("Pasif birime rol atanamaz");

            var roleEntity = await _cmsContext.OrgUnitRoles.AsNoTracking().FirstOrDefaultAsync(o => o.Name == roleType.ToString());
            if (roleEntity == null)
                return Result.NotFound("Rol bulunamadı");

            // DÜZELTİLDİ: IsActive filtresi + CreateDate'e göre en güncel satır garantisi eklendi
            var assignerUserOrgUnitRole = await _cmsContext.OrgUnitUserRoles
                .AsNoTracking()
                .Include(ousr => ousr.OrgUnit)
                .Include(ousr => ousr.Role)
                .Where(ousr =>
                    ousr.CompanyId == companyId &&
                    ousr.UserId == userId &&
                    ousr.IsActive)
                .OrderByDescending(ousr => ousr.CreateDate)
                .FirstOrDefaultAsync();

            bool isSuperAdmin = assignerRole == RoleTypes.SuperAdmin.ToString();

            bool isHigherManager = assignerUserOrgUnitRole != null
                && assignerUserOrgUnitRole.Role.Name == OrgUnitRoleTypes.Manager.ToString()
                && orgUnit.Path.StartsWith(assignerUserOrgUnitRole.OrgUnit.Path);

            if (roleType == OrgUnitRoleTypes.Manager && isHigherManager)
            {
                if (orgUnit.Path == assignerUserOrgUnitRole!.OrgUnit.Path)
                {
                    isHigherManager = false; // Kendi birimi olduğu için yetkiyi iptal et
                }
            }

            if (!isSuperAdmin && !isHigherManager)
                return Result.Forbidden("Bu işlemi yapamazsınız");

            // DÜZELTİLDİ: aynı filtre burada da eklendi
            var exists = await _cmsContext.OrgUnitUserRoles
                .Where(o =>
                    o.CompanyId == companyId &&
                    o.UserId == user.PublicId &&
                    o.IsActive)
                .OrderByDescending(o => o.CreateDate)
                .FirstOrDefaultAsync();

            if (exists != null)
                exists.IsActive = false;

            var orgUnitUserRole = new OrgUnitUserRole
            {
                CompanyId = companyId.Value,
                OrgUnitId = orgUnit.Id,
                UserId = user.PublicId,
                RoleId = roleEntity.Id,
                IsActive = true,
                CreateDate = DateTime.UtcNow
            };

            await _cmsContext.OrgUnitUserRoles.AddAsync(orgUnitUserRole);
            await _cmsContext.SaveChangesAsync();

            // KALDIRILDI: user.OrgUnitId = orgUnit.PublicId; ve buna bağlı _masterContext.SaveChangesAsync()
            // "hangi birimde çalışıyor" ile "hangi birimde onay yetkisi var" artık birbirine karışmıyor.
            // Ayrıca bu satırların kaldırılmasıyla Master/CMS arası atomiklik sorunu da kendiliğinden ortadan kalktı.

            return Result.Success($"Kullanıcı başarıyla birime {roleType} olarak atandı");
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

            if (dto.ParentPublicId.HasValue && dto.ParentPublicId.Value != Guid.Empty)
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
                    CalculateLevel(orgUnit.Path),
                    orgUnit.IsActive,
                    0), "Departman oluşturuldu");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Departman oluşturulamadı");
                return Result<OrgUnitDTO>.Fail("Departman oluşturulurken bir hata oluştu", ResultStatus.Error);
            }
        }

        public async Task<Result<OrgUnitDTO>> UpdateAsync(UpdateOrgUnitDTO dto)
        {
            var userId = _currentUser.UserId;
            var role = _currentUser.Role;

            if (userId == null)
                return Result<OrgUnitDTO>.Unauthorized("Geçersiz oturum");

            var name = dto.Name?.Trim();

            if (string.IsNullOrWhiteSpace(name))
                return Result<OrgUnitDTO>.Fail("Birim adı boş olamaz");

            var orgUnit = await _cmsContext.OrgUnits
                .FirstOrDefaultAsync(o => o.PublicId == dto.OrgUnitPublicId);

            if (orgUnit == null)
                return Result<OrgUnitDTO>.NotFound("Birim bulunamadı");

            bool isSuperAdmin = role == RoleTypes.SuperAdmin.ToString();
            bool isManager = await _orgUnitAuthService.IsAuthorizedAsync(
                userId.Value, orgUnit.Id, OrgUnitRoleTypes.Manager);

            if (!isSuperAdmin && !isManager)
                return Result<OrgUnitDTO>.Forbidden("Bu işlemi yapamazsınız");

            orgUnit.Name = name;
            await _cmsContext.SaveChangesAsync();

            _logger.LogInformation("Birim güncellendi. Id={Id}, Ad={Name}", orgUnit.Id, name);

            var memberCount = await _cmsContext.OrgUnitUserRoles
                .CountAsync(r => r.OrgUnitId == orgUnit.Id && r.IsActive);

            return Result<OrgUnitDTO>.Success(
                new OrgUnitDTO(orgUnit.PublicId, orgUnit.Name, CalculateLevel(orgUnit.Path), orgUnit.IsActive, memberCount),
                "Birim güncellendi");
        }

        public async Task<Result<OrgUnitDTO>> ToggleActiveAsync(ToggleOrgUnitDTO dto)
        {
            var userId = _currentUser.UserId;
            var role = _currentUser.Role;

            if (userId == null)
                return Result<OrgUnitDTO>.Unauthorized("Geçersiz oturum");

            if (role != RoleTypes.SuperAdmin.ToString())
                return Result<OrgUnitDTO>.Forbidden("Yalnızca organizasyon sahibi birim durumunu değiştirebilir");

            var orgUnit = await _cmsContext.OrgUnits
                .FirstOrDefaultAsync(o => o.PublicId == dto.OrgUnitPublicId);

            if (orgUnit == null)
                return Result<OrgUnitDTO>.NotFound("Birim bulunamadı");

            var memberCount = await _cmsContext.OrgUnitUserRoles
                .CountAsync(r => r.OrgUnitId == orgUnit.Id && r.IsActive);

            // Pasifleştirirken alt ağacı da pasifleştir
            if (orgUnit.IsActive)
            {
                var affected = await _cmsContext.OrgUnits
                    .Where(o => EF.Functions.Like(o.Path, orgUnit.Path + "%"))
                    .ExecuteUpdateAsync(s => s.SetProperty(o => o.IsActive, false));

                _logger.LogInformation(
                    "Birim ve alt birimleri pasifleştirildi. Id={Id}, Adet={Count}",
                    orgUnit.Id, affected);

                return Result<OrgUnitDTO>.Success(
                    new OrgUnitDTO(orgUnit.PublicId, orgUnit.Name, CalculateLevel(orgUnit.Path), false, memberCount),
                    affected > 1
                        ? $"Birim ve {affected - 1} alt birimi pasifleştirildi"
                        : "Birim pasifleştirildi");
            }

            // Aktifleştirirken üst birimin aktif olması gerekiyor
            if (orgUnit.ParentId != null)
            {
                var parent = await _cmsContext.OrgUnits
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == orgUnit.ParentId);

                if (parent != null && !parent.IsActive)
                    return Result<OrgUnitDTO>.Conflict(
                        $"Önce üst birim ({parent.Name}) aktifleştirilmeli");
            }

            orgUnit.IsActive = true;
            await _cmsContext.SaveChangesAsync();

            _logger.LogInformation("Birim aktifleştirildi. Id={Id}", orgUnit.Id);

            return Result<OrgUnitDTO>.Success(
                new OrgUnitDTO(orgUnit.PublicId, orgUnit.Name, CalculateLevel(orgUnit.Path), true, memberCount),
                "Birim aktifleştirildi");
        }

        public async Task<Result<List<OrgUnitDTO>>> GetAllAsync()
        {
            var companyId = _currentUser.CompanyId;
            var userId = _currentUser.UserId;

            if (companyId == null || userId == null)
                return Result<List<OrgUnitDTO>>.Unauthorized("Geçersiz oturum");

            var rows = await _cmsContext.OrgUnits
                .AsNoTracking()
                .OrderBy(d => d.Path)
                .Select(d => new
                {
                    d.PublicId,
                    d.Name,
                    d.Path,
                    d.IsActive,
                    MemberCount = _cmsContext.OrgUnitUserRoles
                        .Count(r => r.OrgUnitId == d.Id && r.IsActive)
                })
                .ToListAsync();

            var list = rows
                .Select(d => new OrgUnitDTO(
                    d.PublicId,
                    d.Name,
                    CalculateLevel(d.Path),
                    d.IsActive,
                    d.MemberCount))
                .ToList();

            return Result<List<OrgUnitDTO>>.Success(list);
        }

        public async Task<Result<OrgUnitDTO>> GetById(Guid orgUnitPublicId)
        {
            var companyId = _currentUser.CompanyId;
            var userId = _currentUser.UserId;

            if (companyId == null || userId == null)
                return Result<OrgUnitDTO>.Unauthorized("Geçersiz oturum");

            var orgUnit = await _cmsContext.OrgUnits
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.PublicId == orgUnitPublicId);

            if (orgUnit == null)
                return Result<OrgUnitDTO>.NotFound("Birim bulunamadı");

            return Result<OrgUnitDTO>.Success(new OrgUnitDTO(
                orgUnit.PublicId,
                orgUnit.Name,
                CalculateLevel(orgUnit.Path),
                orgUnit.IsActive,
                _cmsContext.OrgUnitUserRoles
                    .Count(r => r.OrgUnitId == orgUnit.Id && r.IsActive)
            ));
        }

        public async Task<Result<List<OrgUnitMemberDTO>>> GetMembersAsync(Guid orgUnitPublicId)
        {
            var companyId = _currentUser.CompanyId;

            if (companyId == null)
                return Result<List<OrgUnitMemberDTO>>.Unauthorized("Geçersiz oturum");

            var orgUnit = await _cmsContext.OrgUnits
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.PublicId == orgUnitPublicId);

            if (orgUnit == null)
                return Result<List<OrgUnitMemberDTO>>.NotFound("Birim bulunamadı");

            var rows = await _cmsContext.OrgUnitUserRoles
                .AsNoTracking()
                .Where(r => r.OrgUnitId == orgUnit.Id && r.IsActive)
                .Select(r => new
                {
                    r.UserId,
                    RoleName = r.Role.Name,
                    r.CreateDate
                })
                .ToListAsync();

            if (rows.Count == 0)
                return Result<List<OrgUnitMemberDTO>>.Success(new List<OrgUnitMemberDTO>());

            // Kullanıcı bilgileri Master DB'de
            var userIds = rows.Select(r => r.UserId).ToList();

            var users = await _masterContext.AppUsers
                .AsNoTracking()
                .Where(u => userIds.Contains(u.PublicId))
                .Select(u => new { u.PublicId, u.Name, u.Email })
                .ToDictionaryAsync(u => u.PublicId);

            var list = rows
                .Select(r =>
                {
                    var user = users.GetValueOrDefault(r.UserId);
                    return new OrgUnitMemberDTO(
                        r.UserId,
                        user?.Name ?? "—",
                        user?.Email ?? "—",
                        r.RoleName,
                        r.CreateDate);
                })
                .OrderBy(m => m.UserName)
                .ToList();

            return Result<List<OrgUnitMemberDTO>>.Success(list);
        }

        private int CalculateLevel(string path)
        {
            return path.Count(c => c == '/') - 2;
        }
    }
}