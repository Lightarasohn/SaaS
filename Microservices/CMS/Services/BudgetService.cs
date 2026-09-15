using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SaaS.Database.Contexts.CMS;
using SaaS.DTOs;
using SaaS.Interfaces;
using SaaS.Microservices.CMS.DTOs.BudgetDTOs;
using SaaS.Microservices.CMS.Interfaces;
using SaaS.Microservices.CMS.Models;
using SaaS.Microservices.CMS.Utils;
using SaaS.Utils;

namespace SaaS.Microservices.CMS.Services
{
    public class BudgetService : IBudgetService
    {
        private readonly CMSContext _context;
        private readonly ICurrentUser _currentUser;
        private readonly IOrgUnitAuthorizationService _orgUnitAuthService;
        private readonly ILogger<BudgetService> _logger;

        public BudgetService(CMSContext context, ICurrentUser currentUser, IOrgUnitAuthorizationService orgUnitAuthService, ILogger<BudgetService> logger)
        {
            _context = context;
            _currentUser = currentUser;
            _orgUnitAuthService = orgUnitAuthService;
            _logger = logger;
        }

        public async Task<Result<List<BudgetDTO>>> GetAllAsync(int? year, int? month, Guid? orgUnitPublicId)
        {
            var query = _context.Budgets.AsNoTracking().AsQueryable();

            if (year != null)
                query = query.Where(b => b.Year == year);

            if (month != null)
                query = query.Where(b => b.Month == month);

            if (orgUnitPublicId != null)
                query = query.Where(b => b.OrgUnit.PublicId == orgUnitPublicId);

            var list = await query
                .OrderByDescending(b => b.Year)
                .ThenByDescending(b => b.Month)
                .ThenBy(b => b.OrgUnit.Path)
                .Select(b => new BudgetDTO(
                    b.PublicId,
                    b.OrgUnit.PublicId,
                    b.OrgUnit.Name,
                    b.OrgUnit.IsActive,
                    b.Month,
                    b.Year,
                    b.TotalAmount,
                    b.UsedAmount,
                    b.TotalAmount - b.UsedAmount))
                .ToListAsync();

            return Result<List<BudgetDTO>>.Success(list);
        }

        public async Task<Result<BudgetDTO>> CreateAsync(CreateBudgetDTO dto)
        {
            var companyId = _currentUser.CompanyId;
            var userId = _currentUser.UserId;
            var role = _currentUser.Role;

            if (companyId == null || userId == null)
                return Result<BudgetDTO>.Unauthorized("Geçersiz oturum");

            var orgUnit = await _context.OrgUnits
                .FirstOrDefaultAsync(o => o.PublicId == dto.OrgUnitPublicId);

            if (orgUnit == null)
                return Result<BudgetDTO>.NotFound("Birim bulunamadı");

            bool isCompanyAdmin = role == RoleTypes.Admin.ToString() || role == RoleTypes.SuperAdmin.ToString();
            bool isUnitManager = await _orgUnitAuthService.IsAuthorizedAsync(userId.Value, orgUnit.Id, OrgUnitRoleTypes.Manager);

            if (!isCompanyAdmin && !isUnitManager)
                return Result<BudgetDTO>.Forbidden("Bu işlemi yapamazsınız");

            if (!orgUnit.IsActive)
                return Result<BudgetDTO>.Fail("Pasif birime bütçe tanımlanamaz");

            bool exists = await _context.Budgets.AnyAsync(b =>
                b.OrgUnitId == orgUnit.Id &&
                b.Year == dto.Year &&
                b.Month == dto.Month);

            if (exists)
                return Result<BudgetDTO>.Conflict("Bu birim için bu döneme ait bütçe zaten tanımlı");

            var budget = new Budget
            {
                CompanyId = companyId.Value,
                OrgUnitId = orgUnit.Id,
                Month = dto.Month,
                Year = dto.Year,
                TotalAmount = dto.TotalAmount,
                UsedAmount = 0,
                IsDeleted = false,
                CreateUser = userId.Value,
                CreateDate = DateTime.UtcNow
            };

            await _context.Budgets.AddAsync(budget);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Bütçe oluşturuldu. BudgetId={BudgetId}, OrgUnitId={OrgUnitId}, Dönem={Year}/{Month}, Tutar={Amount}",
                budget.Id, orgUnit.Id, budget.Year, budget.Month, budget.TotalAmount);

            return Result<BudgetDTO>.Success(
                new BudgetDTO(
                    budget.PublicId,
                    orgUnit.PublicId,
                    orgUnit.Name,
                    orgUnit.IsActive,
                    budget.Month,
                    budget.Year,
                    budget.TotalAmount,
                    budget.UsedAmount,
                    budget.TotalAmount),
                "Bütçe oluşturuldu");
        }

        public async Task<Result> CanUpdateBudgetAsync(CanUpdateBudgetDTO dto)
        {
            var userId = _currentUser.UserId;
            var role = _currentUser.Role;

            if (userId == null)
                return Result.Unauthorized("Geçersiz oturum");

            var budget = await _context.Budgets
                .AsNoTracking()
                .Include(b => b.OrgUnit)
                .FirstOrDefaultAsync(b => b.PublicId == dto.BudgetPublicId);

            if (budget == null)
                return Result.NotFound("Bütçe bulunamadı");

            if (!budget.OrgUnit.IsActive)
                return Result.Fail("Pasif birimin bütçesi güncellenemez");

            bool isCompanyAdmin = role == RoleTypes.Admin.ToString() || role == RoleTypes.SuperAdmin.ToString();
            bool isUnitManager = await _orgUnitAuthService.IsAuthorizedAsync(userId.Value, budget.OrgUnitId, OrgUnitRoleTypes.Manager);

            if (!isCompanyAdmin && !isUnitManager)
                return Result.Forbidden("Bu işlemi yapamazsınız");

            if (!budget.OrgUnit.IsActive)
                return Result.Fail("Pasif birimin bütçesi güncellenemez");

            bool exists = await _context.Budgets.AsNoTracking().AnyAsync(b =>
                b.Id != budget.Id &&
                b.OrgUnitId == budget.OrgUnitId &&
                b.Year == dto.Year &&
                b.Month == dto.Month);

            if (exists)
                return Result.Conflict("Bu birim için bu döneme ait bütçe zaten tanımlı");

            if (dto.TotalAmount < budget.UsedAmount)
                return Result.Conflict(
                    $"Yeni bütçe ({dto.TotalAmount:N2}) harcanan tutardan ({budget.UsedAmount:N2}) küçük olamaz");

            return Result.Success("Bütçe güncellenebilir");
        }

        public async Task<Result<BudgetDTO>> UpdateBudgetAsync(UpdateBudgetDTO dto)
        {
            var userId = _currentUser.UserId;
            var role = _currentUser.Role;

            if (userId == null)
                return Result<BudgetDTO>.Unauthorized("Geçersiz oturum");

            var budget = await _context.Budgets
                .Include(b => b.OrgUnit)
                .FirstOrDefaultAsync(b => b.PublicId == dto.BudgetPublicId);

            if (budget == null)
                return Result<BudgetDTO>.NotFound("Bütçe bulunamadı");

            if (!budget.OrgUnit.IsActive)
                return Result<BudgetDTO>.Fail("Pasif birimin bütçesi güncellenemez");

            bool isCompanyAdmin = role == RoleTypes.Admin.ToString() || role == RoleTypes.SuperAdmin.ToString();
            bool isUnitManager = await _orgUnitAuthService.IsAuthorizedAsync(userId.Value, budget.OrgUnitId, OrgUnitRoleTypes.Manager);

            if (!isCompanyAdmin && !isUnitManager)
                return Result<BudgetDTO>.Forbidden("Bu işlemi yapamazsınız");

            if (!budget.OrgUnit.IsActive)
                return Result<BudgetDTO>.Fail("Pasif birimin bütçesi güncellenemez");

            // Onaylanmış masrafların toplamının altına inilemez
            if (dto.TotalAmount < budget.UsedAmount)
                return Result<BudgetDTO>.Conflict(
                    $"Yeni bütçe ({dto.TotalAmount:N2}) harcanan tutardan ({budget.UsedAmount:N2}) küçük olamaz");

            // Aynı birimin aynı dönemine ait başka bir bütçe olamaz (kendisi hariç)
            bool exists = await _context.Budgets.AnyAsync(b =>
                b.Id != budget.Id &&
                b.OrgUnitId == budget.OrgUnitId &&
                b.Year == dto.Year &&
                b.Month == dto.Month);

            if (exists)
                return Result<BudgetDTO>.Conflict("Bu birim için bu döneme ait bütçe zaten tanımlı");

            budget.Year = dto.Year;
            budget.Month = dto.Month;
            budget.TotalAmount = dto.TotalAmount;
            budget.UpdateUser = userId.Value;
            budget.UpdateDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Bütçe güncellendi. BudgetId={BudgetId}, Dönem={Year}/{Month}, Tutar={Amount}, UserId={UserId}",
                budget.Id, budget.Year, budget.Month, budget.TotalAmount, userId);

            return Result<BudgetDTO>.Success(
                new BudgetDTO(
                    budget.PublicId,
                    budget.OrgUnit.PublicId,
                    budget.OrgUnit.Name,
                    budget.OrgUnit.IsActive,
                    budget.Month,
                    budget.Year,
                    budget.TotalAmount,
                    budget.UsedAmount,
                    budget.TotalAmount - budget.UsedAmount),
                "Bütçe güncellendi");
        }

        public async Task<Result<BudgetDTO>> GetByIdAsync(Guid budgetPublicId)
        {
            var companyId = _currentUser.CompanyId;
            var userId = _currentUser.UserId;

            if (companyId == null || userId == null)
                return Result<BudgetDTO>.Unauthorized("Geçersiz oturum");
            
            var budget = await _context.Budgets
                .AsNoTracking()
                .Include(b => b.OrgUnit)
                .FirstOrDefaultAsync(b =>
                    b.PublicId == budgetPublicId &&
                    b.IsDeleted == false
                );

            if (budget == null)
                return Result<BudgetDTO>.NotFound("Bütçe bulunamadı");

            return Result<BudgetDTO>.Success(new BudgetDTO(
                budget.PublicId,
                budget.OrgUnit.PublicId,
                budget.OrgUnit.Name,
                budget.OrgUnit.IsActive,
                budget.Month,
                budget.Year,
                budget.TotalAmount,
                budget.UsedAmount,
                budget.TotalAmount - budget.UsedAmount
            ));
        }
    }
}