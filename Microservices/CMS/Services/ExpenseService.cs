using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SaaS.Database.Contexts.CMS;
using SaaS.Database.Contexts.Master;
using SaaS.DTOs;
using SaaS.Interfaces;
using SaaS.Microservices.CMS.DTOs.ExpenseDTOs;
using SaaS.Microservices.CMS.Interfaces;
using SaaS.Microservices.CMS.Models;
using SaaS.Utils;

namespace SaaS.Microservices.CMS.Services
{
    public class ExpenseService : IExpenseService
    {
        private const string STATUS_NAME_PENDING = "Beklemede";

        private readonly MasterContext _masterContext;
        private readonly CMSContext _cmsContext;
        private readonly ILogger<ExpenseService> _logger;
        private readonly ICurrentUser _currentUser;

        public ExpenseService(
            MasterContext masterContext,
            CMSContext cmsContext,
            ILogger<ExpenseService> logger,
            ICurrentUser currentUser)
        {
            _cmsContext = cmsContext;
            _masterContext = masterContext;
            _logger = logger;
            _currentUser = currentUser;
        }

        public async Task<Result> ApproveAsync(Guid expensePublicId)
        {
            var userId = _currentUser.UserId;

            if (userId == null)
                return Result.Unauthorized("Geçersiz Oturum");

            var role = _currentUser.Role;

            if (role != RoleTypes.Admin.ToString() && role != RoleTypes.SuperAdmin.ToString())
                return Result.Forbidden("Bu işlemi yapamazsınız");

            await using var transaction = await _cmsContext.Database.BeginTransactionAsync();
            try
            {
                var expense = await _cmsContext.Expenses
                    .Include(e => e.Budget)
                    .FirstOrDefaultAsync(e => e.PublicId == expensePublicId);

                if (expense == null || expense.IsDeleted)
                    return Result.NotFound("Masraf bulunamadı");

                if (expense.StatusId != (int)ExpenseStatusTypes.Pending)
                    return Result.Conflict("Bu masraf zaten işleme alınmış");

                var budget = expense.Budget;

                if (budget == null || budget.IsDeleted)
                    return Result.NotFound("Bütçe bulunamadı");

                expense.StatusId = (int)ExpenseStatusTypes.Approved;
                expense.UpdateUser = userId.Value;
                expense.UpdateDate = DateTime.UtcNow;

                budget.UsedAmount += expense.Amount;

                await _cmsContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Masraf Onaylandı. ExpenseId={ExpenseId} UserId={userId}", expense.PublicId, userId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex,"Masraf onaylanamadı");
                return Result.Fail("Masraf onaylanırken bir hata oluştu", ResultStatus.Error);
            }
        }

        public async Task<Result> CanApproveAsync(Guid expensePublicId)
        {
            var userId = _currentUser.UserId;

            if (userId == null)
                return Result.Unauthorized("Geçersiz Oturum");

            var role = _currentUser.Role;

            if (role != RoleTypes.Admin.ToString() && role != RoleTypes.SuperAdmin.ToString())
                return Result.Forbidden("Bu işlemi yapamazsınız");

            var expense = await _cmsContext.Expenses
                .Include(e => e.Budget)
                .FirstOrDefaultAsync(e => e.PublicId == expensePublicId);

            if (expense == null || expense.IsDeleted)
                return Result.NotFound("Masraf bulunamadı");

            var budget = expense.Budget;

            if (budget == null || budget.IsDeleted)
                return Result.NotFound("Bütçe bulunamadı");

            if (budget.TotalAmount - budget.UsedAmount - expense.Amount < 0)
            {
                return Result.NotModified("Dikkat bütçe aşılıyor");
            }

            return Result.Success();
        }

        public async Task<Result<ExpenseDTO>> CreateAsync(CreateExpenseDTO dto)
        {
            var companyId = _currentUser.CompanyId;
            var userId = _currentUser.UserId;

            if (companyId == null || userId == null)
                return Result<ExpenseDTO>.Unauthorized("Geçersiz oturum");

            var budget = await _cmsContext.Budgets
                .AsNoTracking()
                .Include(b => b.OrgUnit)
                .FirstOrDefaultAsync(b => b.PublicId == dto.BudgetPublicId);

            if (budget == null)
                return Result<ExpenseDTO>.NotFound("Bütçe bulunamadı");

            var expenseCategory = await _cmsContext.ExpenseCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(ec => ec.PublicId == dto.ExpenseCategoryPublicId);

            if (expenseCategory == null)
                return Result<ExpenseDTO>.NotFound("Masraf kategorisi bulunamadı");

            if (!expenseCategory.IsActive)
                return Result<ExpenseDTO>.Fail("Pasif bir kategoriye masraf eklenemez");

            var expense = new Expense
            {
                CompanyId = companyId.Value,
                UserId = userId.Value,
                BudgetId = budget.Id,
                ExpenseCategoryId = expenseCategory.Id,
                Amount = dto.Amount,
                Description = dto.Description,
                StatusId = (int)ExpenseStatusTypes.Pending,
                IsDeleted = false,
                CreateUser = userId.Value,
                CreateDate = DateTime.UtcNow,
                RejectReason = null
            };

            await _cmsContext.Expenses.AddAsync(expense);
            await _cmsContext.SaveChangesAsync();

            _logger.LogInformation(
                "Masraf eklendi. ExpenseId={ExpenseId}, UserId={UserId}, OrgUnit={OrgUnit}, Tutar={Amount}",
                expense.Id, userId, budget.OrgUnit.Name, expense.Amount);

            return Result<ExpenseDTO>.Success(
                new ExpenseDTO(
                    expense.PublicId,
                    userId.Value,
                    budget.PublicId,
                    expenseCategory.PublicId,
                    expense.StatusId,
                    expenseCategory.Name,
                    expense.Amount,
                    expense.Description,
                    STATUS_NAME_PENDING,
                    _currentUser.Name ?? "—",
                    expense.CreateDate,
                    budget.OrgUnit.Name),
                "Masraf eklendi");
        }

        public async Task<Result<List<ExpenseDTO>>> GetAllAsync(
            Guid? budgetPublicId,
            int? statusId,
            bool onlyMine)
        {
            var query = _cmsContext.Expenses.AsNoTracking().AsQueryable();

            if (budgetPublicId != null)
                query = query.Where(e => e.Budget.PublicId == budgetPublicId);

            if (statusId != null)
                query = query.Where(e => e.StatusId == statusId);

            if (onlyMine)
            {
                var userId = _currentUser.UserId;
                if (userId == null)
                    return Result<List<ExpenseDTO>>.Unauthorized("Geçersiz oturum");

                query = query.Where(e => e.UserId == userId);
            }

            var rows = await query
                .OrderByDescending(e => e.CreateDate)
                .Select(e => new
                {
                    e.PublicId,
                    e.UserId,
                    BudgetPublicId = e.Budget.PublicId,
                    CategoryPublicId = e.ExpenseCategory.PublicId,
                    CategoryName = e.ExpenseCategory.Name,
                    e.StatusId,
                    StatusName = e.Status.Name,
                    e.Amount,
                    e.Description,
                    e.CreateDate,
                    OrgUnitName = e.Budget.OrgUnit.Name
                })
                .ToListAsync();

            // Kullanıcı adları Master DB'de: ayrı sorgu, bellekte eşleştirme
            var userIds = rows.Select(r => r.UserId).Distinct().ToList();

            var names = await _masterContext.AppUsers
                .AsNoTracking()
                .Where(u => userIds.Contains(u.PublicId))
                .ToDictionaryAsync(u => u.PublicId, u => u.Name);

            var list = rows
                .Select(r => new ExpenseDTO(
                    r.PublicId,
                    r.UserId,
                    r.BudgetPublicId,
                    r.CategoryPublicId,
                    r.StatusId,
                    r.CategoryName,
                    r.Amount,
                    r.Description,
                    r.StatusName,
                    names.GetValueOrDefault(r.UserId, "—"),
                    r.CreateDate,
                    r.OrgUnitName))
                .ToList();

            return Result<List<ExpenseDTO>>.Success(list);
        }

        public async Task<Result> RejectAsync(Guid expensePublicId, RejectExpenseDTO dto)
        {
            var userId = _currentUser.UserId;

            if (userId == null)
                return Result.Unauthorized("Geçersiz Oturum");

            var role = _currentUser.Role;

            if (role != RoleTypes.Admin.ToString() && role != RoleTypes.SuperAdmin.ToString())
                return Result.Forbidden("Bu işlemi yapamazsınız");

                var expense = await _cmsContext.Expenses
                    .FirstOrDefaultAsync(e => e.PublicId == expensePublicId);

                if (expense == null || expense.IsDeleted)
                    return Result.NotFound("Masraf bulunamadı");

                if (expense.StatusId != (int)ExpenseStatusTypes.Pending)
                    return Result.Conflict("Bu masraf zaten işleme alınmış");

                expense.StatusId = (int)ExpenseStatusTypes.Rejected;
                expense.RejectReason = dto.RejectReason;
                expense.UpdateUser = userId.Value;
                expense.UpdateDate = DateTime.UtcNow;

                await _cmsContext.SaveChangesAsync();

                _logger.LogInformation("Masraf Reddedildi. ExpenseId={ExpenseId} User={userId}", expense.PublicId, userId);

                return Result.Success();
        }
    }
}