using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SaaS.Database.Contexts.CMS;
using SaaS.DTOs;
using SaaS.Interfaces;
using SaaS.Microservices.CMS.DTOs.ExpenseCategoryDTOs;
using SaaS.Microservices.CMS.Interfaces;
using SaaS.Microservices.CMS.Models;

namespace SaaS.Microservices.CMS.Services
{
    public class ExpenseCategoryService : IExpenseCategoryService
    {
        private readonly CMSContext _context;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<ExpenseCategoryService> _logger;

        public ExpenseCategoryService(
            CMSContext context,
            ICurrentUser currentUser,
            ILogger<ExpenseCategoryService> logger)
        {
            _context = context;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Result<List<ExpenseCategoryDTO>>> GetAllAsync()
        {
            var companyId = _currentUser.CompanyId;
            var userId = _currentUser.UserId; 

            if (companyId == null || userId == null)
                return Result<List<ExpenseCategoryDTO>>.Unauthorized("Geçersiz oturum");

            var list = await _context.ExpenseCategories
                .AsNoTracking()
                .OrderBy(ec => ec.Name)
                .Select(ec => new ExpenseCategoryDTO(
                    ec.PublicId,
                    ec.Name,
                    ec.IsActive))
                .ToListAsync();

            return Result<List<ExpenseCategoryDTO>>.Success(list);
        }

        public async Task<Result<ExpenseCategoryDTO>> CreateAsync(CreateExpenseCategoryDTO dto)
        {
            var companyId = _currentUser.CompanyId;

            if (companyId == null)
                return Result<ExpenseCategoryDTO>.Unauthorized("Geçersiz oturum");

            var name = dto.Name?.Trim();

            if (string.IsNullOrWhiteSpace(name))
                return Result<ExpenseCategoryDTO>.Fail("Kategori adı boş olamaz");

            // Aynı isimde kategori olmasın (büyük/küçük harf duyarsız)
            bool exists = await _context.ExpenseCategories
                .AnyAsync(ec => ec.Name.ToLower() == name.ToLower());

            if (exists)
                return Result<ExpenseCategoryDTO>.Conflict("Bu isimde bir kategori zaten var");

            var category = new ExpenseCategory
            {
                CompanyId = companyId.Value,
                Name = name,
                IsActive = true
            };

            await _context.ExpenseCategories.AddAsync(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Masraf kategorisi oluşturuldu. CategoryId={CategoryId}, Ad={Name}",
                category.Id, category.Name);

            return Result<ExpenseCategoryDTO>.Success(
                new ExpenseCategoryDTO(
                    category.PublicId,
                    category.Name,
                    category.IsActive),
                "Kategori oluşturuldu");
        }

        public async Task<Result<ExpenseCategoryDTO>> UpdateAsync(UpdateExpenseCategoryDTO dto)
        {
            var userId = _currentUser.UserId;

            if (userId == null)
                return Result<ExpenseCategoryDTO>.Unauthorized("Geçersiz oturum");

            var name = dto.Name?.Trim();

            if (string.IsNullOrWhiteSpace(name))
                return Result<ExpenseCategoryDTO>.Fail("Kategori adı boş olamaz");

            var category = await _context.ExpenseCategories
                .FirstOrDefaultAsync(ec => ec.PublicId == dto.ExpenseCategoryPublicId);

            if (category == null)
                return Result<ExpenseCategoryDTO>.NotFound("Kategori bulunamadı");

            // Kendisi hariç aynı isimde başka kategori olmasın
            bool exists = await _context.ExpenseCategories
                .AnyAsync(ec => ec.Id != category.Id && ec.Name.ToLower() == name.ToLower());

            if (exists)
                return Result<ExpenseCategoryDTO>.Conflict("Bu isimde bir kategori zaten var");

            category.Name = name;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Masraf kategorisi güncellendi. CategoryId={CategoryId}, Ad={Name}, UserId={UserId}",
                category.Id, category.Name, userId);

            return Result<ExpenseCategoryDTO>.Success(
                new ExpenseCategoryDTO(
                    category.PublicId,
                    category.Name,
                    category.IsActive),
                "Kategori güncellendi");
        }

        public async Task<Result<ExpenseCategoryDTO>> GetByIdAsync(Guid expenseCategoryPublicId)
        {
            var companyId = _currentUser.CompanyId;
            var userId = _currentUser.UserId; 

            if (companyId == null || userId == null)
                return Result<ExpenseCategoryDTO>.Unauthorized("Geçersiz oturum");

            var expenseCategory = await _context.ExpenseCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(ec => ec.PublicId == expenseCategoryPublicId);

            if (expenseCategory == null)
                return Result<ExpenseCategoryDTO>.NotFound("Masraf kategorisi bulunamadı");

            return Result<ExpenseCategoryDTO>.Success(new ExpenseCategoryDTO(
                expenseCategory.PublicId,
                expenseCategory.Name,
                expenseCategory.IsActive
            ));
        }
    }
}