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

namespace SaaS.Microservices.CMS.Services
{
    public class BudgetService : IBudgetService
    {
        private readonly CMSContext _context;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<BudgetService> _logger;
        public BudgetService(CMSContext context, ICurrentUser currentUser, ILogger<BudgetService> logger)
        {
            _context = context;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Result<BudgetDTO>> CreateAsync(CreateBudgetDTO dto)
        {
            var companyId = _currentUser.CompanyId;

            if (companyId == null)
                return Result<BudgetDTO>.Unauthorized("Geçersiz oturum");

            var orgUnit = await _context.OrgUnits.FirstOrDefaultAsync(org => org.PublicId == dto.OrgUnitPublicId);

            if (orgUnit == null)
                return Result<BudgetDTO>.NotFound("Birim bulunamadı");

            if (!orgUnit.IsActive)
                return Result<BudgetDTO>.Fail("Pasif birime bütçe ayarlanamaz");

            bool exists = await _context.Budgets.AnyAsync(b =>
                b.Month == dto.Month &&
                b.Year == dto.Year &&
                b.OrgUnitId == orgUnit.Id);

            if (exists)
                return Result<BudgetDTO>.Conflict("Bu birim için bu döneme ait bütçe zaten tanımlı");

            var budget = new Budget
            {
                CompanyId = companyId.Value,
                OrgUnitId = orgUnit.Id,
                Month = dto.Month,
                Year = dto.Year,
                TotalAmount = dto.TotalAmount,
                IsDeleted = false,
                CreateUser = _currentUser.UserId,
                CreateDate = DateTime.UtcNow
            };

            await _context.Budgets.AddAsync(budget);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Bütçe oluşturuldu. Id={Id}, OrgUnitId={OrgUnitId}, Dönem={Year}/{Month}",
        budget.Id, orgUnit.Id, dto.Year, dto.Month);

            return Result<BudgetDTO>.Success(new BudgetDTO(
                budget.PublicId,
                orgUnit.PublicId,
                orgUnit.Name,
                budget.Month,
                budget.Year,
                budget.TotalAmount,
                budget.UsedAmount,
                budget.TotalAmount
            ), "Bütçe Oluşturuldu");
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
                    b.Month, 
                    b.Year, 
                    b.TotalAmount, 
                    b.UsedAmount, 
                    b.TotalAmount - b.UsedAmount))
                .ToListAsync();

            return Result<List<BudgetDTO>>.Success(list);
        }
    }
}