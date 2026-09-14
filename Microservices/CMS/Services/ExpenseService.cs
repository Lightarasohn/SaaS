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
using SaaS.Microservices.CMS.Utils;
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
        private readonly IConfiguration _configuration;
        private readonly IOrgUnitAuthorizationService _orgUnitAuthService;
        private int _maxCreateRange;
        private int _maxUpdateRange;
        public ExpenseService(
            MasterContext masterContext,
            CMSContext cmsContext,
            ILogger<ExpenseService> logger,
            ICurrentUser currentUser,
            IConfiguration configuration,
            IOrgUnitAuthorizationService orgUnitAuthService)
        {
            _cmsContext = cmsContext;
            _masterContext = masterContext;
            _logger = logger;
            _currentUser = currentUser;
            _configuration = configuration;
            _maxCreateRange = _configuration.GetSection("CRUDSettings").GetValue<int>("MaxCreateRange");
            _maxUpdateRange = _configuration.GetSection("CRUDSettings").GetValue<int>("MaxUpdateRange");
            _orgUnitAuthService = orgUnitAuthService;
        }

        public async Task<Result> ApproveAsync(ApproveExpenseDTO dto)
        {
            var userId = _currentUser.UserId;

            if (userId == null)
                return Result.Unauthorized("Geçersiz Oturum");

            var expense = await _cmsContext.Expenses
                    .Include(e => e.Budget)
                    .FirstOrDefaultAsync(e => e.PublicId == dto.ExpensePublicId);

            if (expense == null || expense.IsDeleted)
                return Result.NotFound("Masraf bulunamadı");

            if (expense.StatusId != (int)ExpenseStatusTypes.Pending)
                return Result.Conflict("Bu masraf zaten işleme alınmış");

            var budget = expense.Budget;

            if (budget == null || budget.IsDeleted)
                return Result.NotFound("Bütçe bulunamadı");

            var role = _currentUser.Role;

            var isCompanyAdmin = role == RoleTypes.Admin.ToString() || role == RoleTypes.SuperAdmin.ToString();
            var isUnitApprover = await _orgUnitAuthService.IsAuthorizedAsync(
                userId.Value, expense.Budget.OrgUnitId, OrgUnitRoleTypes.Manager, OrgUnitRoleTypes.Approver);

            if (!isCompanyAdmin && !isUnitApprover)
                return Result.Forbidden("Bu işlemi yapamazsınız");

            await using var transaction = await _cmsContext.Database.BeginTransactionAsync();
            try
            {
                expense.StatusId = (int)ExpenseStatusTypes.Approved;
                expense.UpdateUser = userId.Value;
                expense.UpdateDate = DateTime.UtcNow;
                await _cmsContext.SaveChangesAsync();

                await _cmsContext.Budgets
                    .Where(b => b.Id == expense.BudgetId)
                    .ExecuteUpdateAsync(s => s.SetProperty(
                        b => b.UsedAmount,
                        b => b.UsedAmount + expense.Amount));

                await transaction.CommitAsync();

                _logger.LogInformation("Masraf Onaylandı. ExpenseId={ExpenseId} UserId={userId}", expense.PublicId, userId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Masraf onaylanamadı");
                return Result.Fail("Masraf onaylanırken bir hata oluştu", ResultStatus.Error);
            }
        }

        public async Task<Result<List<Guid>>> ApproveRangeAsync(ApproveRangeExpenseDTO dto)
        {
            var userId = _currentUser.UserId;
            if (userId == null)
                return Result<List<Guid>>.Unauthorized("Geçersiz oturum");

            var ids = dto.ExpensePublicIdList;

            if (ids == null || ids.Count == 0)
                return Result<List<Guid>>.Fail("Boş liste gönderilemez");

            if (ids.Count > _maxUpdateRange)
                return Result<List<Guid>>.Fail(
                    $"Masraf onay listesi en fazla {_maxUpdateRange} kalem içerebilir");

            if (ids.Distinct().Count() != ids.Count)
                return Result<List<Guid>>.Fail("Bir masrafı sadece bir kez işleme alabilirsiniz");

            var expenses = await _cmsContext.Expenses
                .Include(e => e.Budget)
                .Where(e => ids.Contains(e.PublicId))
                .ToListAsync();

            // 1. Bulunamayanlar
            var foundIds = expenses.Select(e => e.PublicId).ToHashSet();
            var missingIds = ids.Where(id => !foundIds.Contains(id)).ToList();

            if (missingIds.Count > 0)
                return Result<List<Guid>>.Invalid(missingIds, "Bazı masraflar bulunamadı");

            // 2. Beklemede olmayanlar
            var notPending = expenses
                .Where(e => e.StatusId != (int)ExpenseStatusTypes.Pending)
                .Select(e => e.PublicId)
                .ToList();

            if (notPending.Count > 0)
                return Result<List<Guid>>.Invalid(notPending, "Bazı masraflar zaten işleme alınmış");

            // 3. Bütçe başına toplam tutarı hesapla
            var totalsByBudget = expenses
                .GroupBy(e => e.BudgetId)
                .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

            var budgetIds = totalsByBudget.Keys.ToList();

            var budgets = await _cmsContext.Budgets
                .Where(b => budgetIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id);

            var now = DateTime.UtcNow;

            var role = _currentUser.Role;

            await using var transaction = await _cmsContext.Database.BeginTransactionAsync();
            try
            {
                foreach (var expense in expenses)
                {
                    var isCompanyAdmin = role == RoleTypes.Admin.ToString() || role == RoleTypes.SuperAdmin.ToString();
                    var isUnitApprover = await _orgUnitAuthService.IsAuthorizedAsync(
                        userId.Value, expense.Budget.OrgUnitId, OrgUnitRoleTypes.Manager, OrgUnitRoleTypes.Approver);

                    if (!isCompanyAdmin && !isUnitApprover)
                        return Result<List<Guid>>.Forbidden("Bu işlemi yapamazsınız");
                    expense.StatusId = (int)ExpenseStatusTypes.Approved;
                    expense.UpdateUser = userId.Value;
                    expense.UpdateDate = now;
                }

                await _cmsContext.SaveChangesAsync();

                // Her bütçe için tek atomik artırım
                foreach (var (budgetId, total) in totalsByBudget)
                {
                    await _cmsContext.Budgets
                        .Where(b => b.Id == budgetId)
                        .ExecuteUpdateAsync(s => s.SetProperty(
                            b => b.UsedAmount,
                            b => b.UsedAmount + total));
                }


                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Toplu masraf onaylandı. Adet={Count}, UserId={UserId}",
                    expenses.Count, userId);

                return Result<List<Guid>>.Success(
                    expenses.Select(e => e.PublicId).ToList(),
                    $"{expenses.Count} masraf onaylandı");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Toplu masraf onaylanamadı");
                return Result<List<Guid>>.Fail("Masraflar onaylanırken bir hata oluştu", ResultStatus.Error);
            }
        }

        public async Task<Result> CanApproveAsync(Guid expensePublicId)
        {
            var userId = _currentUser.UserId;

            if (userId == null)
                return Result.Unauthorized("Geçersiz Oturum");

            var role = _currentUser.Role;

            var expense = await _cmsContext.Expenses
                .AsNoTracking()
                .Include(e => e.Budget)
                .FirstOrDefaultAsync(e => e.PublicId == expensePublicId);

            if (expense == null || expense.IsDeleted)
                return Result.NotFound("Masraf bulunamadı");

            var isCompanyAdmin = role == RoleTypes.Admin.ToString() || role == RoleTypes.SuperAdmin.ToString();
            var isUnitApprover = await _orgUnitAuthService.IsAuthorizedAsync(
                userId.Value, expense.Budget.OrgUnitId, OrgUnitRoleTypes.Manager, OrgUnitRoleTypes.Approver);

            if (!isCompanyAdmin && !isUnitApprover)
                return Result.Forbidden("Bu işlemi yapamazsınız");

            var budget = expense.Budget;

            if (budget == null || budget.IsDeleted)
                return Result.NotFound("Bütçe bulunamadı");

            if (budget.TotalAmount - budget.UsedAmount - expense.Amount < 0)
            {
                return Result.NotModified("Dikkat bütçe aşılıyor");
            }

            return Result.Success();
        }

        public async Task<Result<List<Guid>>> CanApproveRangeAsync(CanApproveRangeExpenseDTO dto)
        {
            var userId = _currentUser.UserId;
            if (userId == null)
                return Result<List<Guid>>.Unauthorized("Geçersiz oturum");

            var ids = dto.ExpensePublicIdList;

            if (ids == null || ids.Count == 0)
                return Result<List<Guid>>.Fail("Boş liste gönderilemez");

            if (ids.Count > _maxUpdateRange)
                return Result<List<Guid>>.Fail(
                    $"Masraf onay listesi en fazla {_maxUpdateRange} kalem içerebilir");

            if (ids.Distinct().Count() != ids.Count)
                return Result<List<Guid>>.Fail("Bir masrafı sadece bir kez işleme alabilirsiniz");

            var expenses = await _cmsContext.Expenses
                .AsNoTracking()
                .Include(e => e.Budget)
                .Where(e => ids.Contains(e.PublicId))
                .ToListAsync();

            var role = _currentUser.Role;

            foreach (var expense in expenses)
            {
                var isCompanyAdmin = role == RoleTypes.Admin.ToString() || role == RoleTypes.SuperAdmin.ToString();
                var isUnitApprover = await _orgUnitAuthService.IsAuthorizedAsync(
                    userId.Value, expense.Budget.OrgUnitId, OrgUnitRoleTypes.Manager, OrgUnitRoleTypes.Approver);

                if (!isCompanyAdmin && !isUnitApprover)
                    return Result<List<Guid>>.Forbidden("Bu işlemi yapamazsınız");
            }

            // 1. Bulunamayanlar
            var foundIds = expenses.Select(e => e.PublicId).ToHashSet();
            var missingIds = ids.Where(id => !foundIds.Contains(id)).ToList();

            if (missingIds.Count > 0)
                return Result<List<Guid>>.Invalid(missingIds, "Bazı masraflar bulunamadı");

            // 2. Beklemede olmayanlar
            var notPending = expenses
                .Where(e => e.StatusId != (int)ExpenseStatusTypes.Pending)
                .Select(e => e.PublicId)
                .ToList();

            if (notPending.Count > 0)
                return Result<List<Guid>>.Invalid(notPending, "Bazı masraflar zaten işleme alınmış");

            // 3. Bütçe başına toplam tutarı hesapla
            var totalsByBudget = expenses
                .GroupBy(e => e.BudgetId)
                .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

            var budgetIds = totalsByBudget.Keys.ToList();

            var budgets = await _cmsContext.Budgets
                .AsNoTracking()
                .Where(b => budgetIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id);

            var exceededBudgetIds = totalsByBudget
    .Where(kv => budgets[kv.Key].UsedAmount + kv.Value > budgets[kv.Key].TotalAmount)
    .Select(kv => kv.Key)
    .ToList();

            if (exceededBudgetIds.Count > 0)
            {
                var exceededExpenseIds = expenses
                    .Where(e => exceededBudgetIds.Contains(e.BudgetId))
                    .Select(e => e.PublicId)
                    .ToList();

                return Result<List<Guid>>.Invalid(
                    exceededExpenseIds,
                    "Bu masraflar onaylanırsa bütçe aşılacak");
            }

            return Result<List<Guid>>.Success(
                expenses.Select(e => e.PublicId).ToList(),
                "Masraflar onaylanabilir");
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

            ExpenseCategory expenseCategory;

            if (dto.ExpenseCategoryPublicId != null)
            {
                var found = await _cmsContext.ExpenseCategories
                    .FirstOrDefaultAsync(ec => ec.PublicId == dto.ExpenseCategoryPublicId);

                if (found == null)
                    return Result<ExpenseDTO>.NotFound("Masraf kategorisi bulunamadı");

                if (!found.IsActive)
                    return Result<ExpenseDTO>.Fail("Pasif bir kategoriye masraf eklenemez");

                expenseCategory = found;
            }
            else
            {
                var name = dto.ExpenseCategoryName?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    return Result<ExpenseDTO>.Fail("Kategori seçilmeli veya adı belirtilmeli");

                var existing = await _cmsContext.ExpenseCategories
                    .FirstOrDefaultAsync(ec => ec.Name.ToLower() == name.ToLower());

                if (existing != null)
                {
                    if (!existing.IsActive)
                        return Result<ExpenseDTO>.Fail("Pasif bir kategoriye masraf eklenemez");
                    expenseCategory = existing;
                }
                else
                {
                    expenseCategory = new ExpenseCategory { CompanyId = companyId.Value, Name = name, IsActive = true };
                    await _cmsContext.ExpenseCategories.AddAsync(expenseCategory);

                    try
                    {
                        await _cmsContext.SaveChangesAsync();
                    }
                    catch (DbUpdateException)
                    {
                        // Eşzamanlı istek aynı ismi az önce oluşturmuş olabilir
                        var raceWinner = await _cmsContext.ExpenseCategories
                            .FirstOrDefaultAsync(ec => ec.Name.ToLower() == name.ToLower());
                        if (raceWinner == null) throw;
                        expenseCategory = raceWinner;
                    }
                }
            }

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

        public async Task<Result<List<ExpenseDTO>>> CreateRangeAsync(CreateRangeExpenseDTO dto)
        {
            var companyId = _currentUser.CompanyId;
            var userId = _currentUser.UserId;

            if (companyId == null || userId == null)
                return Result<List<ExpenseDTO>>.Unauthorized("Geçersiz oturum");

            if (dto.Items == null || dto.Items.Count == 0)
                return Result<List<ExpenseDTO>>.Fail("Boş masraf listesi gönderilemez");

            if (dto.Items.Count > _maxCreateRange)
                return Result<List<ExpenseDTO>>.Fail(
                    $"Masraf listesi en fazla {_maxCreateRange} kalem içerebilir");

            var budget = await _cmsContext.Budgets
                .AsNoTracking()
                .Include(b => b.OrgUnit)
                .FirstOrDefaultAsync(b => b.PublicId == dto.BudgetPublicId);

            if (budget == null)
                return Result<List<ExpenseDTO>>.NotFound("Bütçe bulunamadı");

            // Tüm kategorileri tek sorguda çek
            var categoryIds = dto.Items.Select(i => i.ExpenseCategoryPublicId).Distinct().ToList();

            var categories = await _cmsContext.ExpenseCategories
                .AsNoTracking()
                .Where(ec => categoryIds.Contains(ec.PublicId))
                .ToDictionaryAsync(ec => ec.PublicId);

            // Eksik veya pasif kategori varsa hiçbirini kaydetme
            var invalid = categoryIds
                .Where(id => !categories.ContainsKey(id) || !categories[id].IsActive)
                .ToList();

            if (invalid.Count > 0)
                return Result<List<ExpenseDTO>>.Fail(
                    $"{invalid.Count} kalemde geçersiz veya pasif kategori var. Hiçbir masraf kaydedilmedi.");

            var now = DateTime.UtcNow;

            var expenses = dto.Items.Select(i => new Expense
            {
                CompanyId = companyId.Value,
                UserId = userId.Value,
                BudgetId = budget.Id,
                ExpenseCategoryId = categories[i.ExpenseCategoryPublicId].Id,
                Amount = i.Amount,
                Description = i.Description,
                StatusId = (int)ExpenseStatusTypes.Pending,
                IsDeleted = false,
                CreateUser = userId.Value,
                CreateDate = now,
                RejectReason = null
            }).ToList();

            await _cmsContext.Expenses.AddRangeAsync(expenses);
            await _cmsContext.SaveChangesAsync();

            _logger.LogInformation(
                "Toplu masraf eklendi. Adet={Count}, BudgetId={BudgetId}, UserId={UserId}",
                expenses.Count, budget.Id, userId);

            // Kategori adlarını sözlükten al — navigasyon yüklenmedi
            var idToCategory = categories.Values.ToDictionary(c => c.Id);

            var list = expenses.Select(e => new ExpenseDTO(
                e.PublicId,
                userId.Value,
                budget.PublicId,
                idToCategory[e.ExpenseCategoryId].PublicId,
                e.StatusId,
                idToCategory[e.ExpenseCategoryId].Name,
                e.Amount,
                e.Description,
                STATUS_NAME_PENDING,
                _currentUser.Name ?? "—",
                e.CreateDate,
                budget.OrgUnit.Name
            )).ToList();

            return Result<List<ExpenseDTO>>.Success(list, $"{list.Count} masraf eklendi");
        }

        public async Task<Result<List<ExpenseDTO>>> GetAllAsync(
    Guid? budgetPublicId,
    int? statusId,
    bool onlyMine)
        {
            var userId = _currentUser.UserId;
            var role = _currentUser.Role;

            if (userId == null)
                return Result<List<ExpenseDTO>>.Unauthorized("Geçersiz oturum");

            bool isCompanyAdmin = role == RoleTypes.Admin.ToString() || role == RoleTypes.SuperAdmin.ToString();

            var query = _cmsContext.Expenses.AsNoTracking().AsQueryable();

            if (!isCompanyAdmin)
            {
                if (onlyMine)
                {
                    query = query.Where(e => e.UserId == userId);
                }
                else
                {
                    var scopedPaths = await _orgUnitAuthService.GetAuthorizedOrgUnitPathsAsync(
                        userId.Value, OrgUnitRoleTypes.Manager, OrgUnitRoleTypes.Approver);

                    // Kendi masrafları + yetkili olduğu birimlerin (ve alt birimlerinin) tüm masrafları
                    query = query.Where(e =>
                        e.UserId == userId ||
                        scopedPaths.Any(p => EF.Functions.Like(e.Budget.OrgUnit.Path, p + "%")));
                }
            }
            // isCompanyAdmin ise hiçbir ek filtre yok — zaten tüm şirketi görüyor

            if (budgetPublicId != null)
                query = query.Where(e => e.Budget.PublicId == budgetPublicId);

            if (statusId != null)
                query = query.Where(e => e.StatusId == statusId);

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

            var userIds = rows.Select(r => r.UserId).Distinct().ToList();

            var names = await _masterContext.AppUsers
                .AsNoTracking()
                .Where(u => userIds.Contains(u.PublicId))
                .ToDictionaryAsync(u => u.PublicId, u => u.Name);

            var list = rows
                .Select(r => new ExpenseDTO(
                    r.PublicId, r.UserId, r.BudgetPublicId, r.CategoryPublicId, r.StatusId,
                    r.CategoryName, r.Amount, r.Description, r.StatusName,
                    names.GetValueOrDefault(r.UserId, "—"), r.CreateDate, r.OrgUnitName))
                .ToList();

            return Result<List<ExpenseDTO>>.Success(list);
        }

        public async Task<Result> RejectAsync(RejectExpenseDTO dto)
        {
            var userId = _currentUser.UserId;

            if (userId == null)
                return Result.Unauthorized("Geçersiz Oturum");

            var role = _currentUser.Role;

            var expense = await _cmsContext.Expenses
                .Include(e => e.Budget)
                .FirstOrDefaultAsync(e => e.PublicId == dto.ExpensePublicId);

            if (expense == null || expense.IsDeleted)
                return Result.NotFound("Masraf bulunamadı");

            if (expense.Budget == null)
                return Result.NotFound("Bütçe bulunamadı");

            var isCompanyAdmin = role == RoleTypes.Admin.ToString() || role == RoleTypes.SuperAdmin.ToString();
            var isUnitApprover = await _orgUnitAuthService.IsAuthorizedAsync(
                userId.Value, expense.Budget.OrgUnitId, OrgUnitRoleTypes.Manager, OrgUnitRoleTypes.Approver);

            if (!isCompanyAdmin && !isUnitApprover)
                return Result.Forbidden("Bu işlemi yapamazsınız");

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

        public async Task<Result<ExpenseDTO>> UpdateExpenseAsync(UpdateExpenseDTO dto)
        {
            var userId = _currentUser.UserId;

            if (userId == null)
                return Result<ExpenseDTO>.Unauthorized("Geçersiz oturum");

            var expense = await _cmsContext.Expenses
                .Include(e => e.Budget).ThenInclude(b => b.OrgUnit)
                .FirstOrDefaultAsync(e => e.PublicId == dto.ExpensePublicId);

            if (expense == null)
                return Result<ExpenseDTO>.NotFound("Masraf bulunamadı");

            var role = _currentUser.Role;

            var isCompanyAdmin = role == RoleTypes.Admin.ToString() || role == RoleTypes.SuperAdmin.ToString();
            var isUnitApprover = await _orgUnitAuthService.IsAuthorizedAsync(
                userId.Value, expense.Budget.OrgUnitId, OrgUnitRoleTypes.Manager, OrgUnitRoleTypes.Approver);

            if (!isCompanyAdmin && !isUnitApprover)
                return Result<ExpenseDTO>.Forbidden("Bu işlemi yapamazsınız");

            // İkinci "sahiplik" kontrolü kaldırıldı — tasarım gereği owner değil, Approver/Admin düzenliyor.

            // İşleme alınmış masraf değiştirilemez
            if (expense.StatusId != (int)ExpenseStatusTypes.Pending)
                return Result<ExpenseDTO>.Conflict("İşleme alınmış masraf düzenlenemez");

            var category = await _cmsContext.ExpenseCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(ec => ec.PublicId == dto.ExpenseCategoryPublicId);

            if (category == null)
                return Result<ExpenseDTO>.NotFound("Masraf kategorisi bulunamadı");

            if (!category.IsActive)
                return Result<ExpenseDTO>.Fail("Pasif bir kategori seçilemez");

            expense.ExpenseCategoryId = category.Id;
            expense.Amount = dto.Amount;
            expense.Description = dto.Description;
            expense.UpdateUser = userId.Value;
            expense.UpdateDate = DateTime.UtcNow;

            await _cmsContext.SaveChangesAsync();

            _logger.LogInformation(
                "Masraf güncellendi. ExpenseId={ExpenseId}, UserId={UserId}, Tutar={Amount}",
                expense.Id, userId, expense.Amount);

            return Result<ExpenseDTO>.Success(
                new ExpenseDTO(
                    expense.PublicId,
                    expense.UserId,
                    expense.Budget.PublicId,
                    category.PublicId,
                    expense.StatusId,
                    category.Name,
                    expense.Amount,
                    expense.Description,
                    STATUS_NAME_PENDING,
                    _currentUser.Name ?? "—",
                    expense.CreateDate,
                    expense.Budget.OrgUnit.Name),
                "Masraf güncellendi");
        }
    }
}