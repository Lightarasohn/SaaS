using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SaaS.Database.Contexts.CMS;
using SaaS.Database.Contexts.Master;
using SaaS.DTOs;
using SaaS.DTOs.UserManagementDTOs;
using SaaS.Interfaces;
using SaaS.Models;
using SaaS.Utils;

namespace SaaS.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly MasterContext _masterContext;
        private readonly ICurrentUser _currentUser;

        public UserManagementService(MasterContext masterContext, ICurrentUser currentUser)
        {
            _masterContext = masterContext;
            _currentUser = currentUser;
        }

        public async Task<Result> ChangeRoleAsync(ChangeUserRoleDTO dto)
        {
            var userId = _currentUser.UserId;
            var companyId = _currentUser.CompanyId;
            var userRole = _currentUser.Role;

            if (userId == null || companyId == null)
                return Result.Unauthorized("Geçersiz Oturum");

            if (userRole == null || userRole == RoleTypes.User.ToString())
                return Result.Forbidden("Bu işlemi yapamazsınız");

            var company = await _masterContext.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.PublicId == companyId);

            if (company == null)
                return Result.NotFound("Organizasyon bulunamadı");

            var user = await _masterContext.AppUsers.FirstOrDefaultAsync(u => u.PublicId == dto.UserPublicId);

            if (user == null)
                return Result.NotFound("Kullanıcı bulunamadı");

            if (user.CompanyId != company.Id)
                return Result.Forbidden("Bu işlemi yapamazsınız");

            var role = await _masterContext.AppRoles.FirstOrDefaultAsync(r => r.Id == dto.RoleId);

            if (role == null)
                return Result.NotFound("Rol bulunamadı");

            if (userRole != RoleTypes.SuperAdmin.ToString())
                return Result.Forbidden("Yalnızca organizasyon sahibi rol değiştirebilir");

            if (user.PublicId == userId)
                return Result.Fail("Kendi rolünüzü değiştiremezsiniz");

            // Son SuperAdmin'i düşürme
            if (user.RoleId == (int)RoleTypes.SuperAdmin && dto.RoleId != (int)RoleTypes.SuperAdmin)
            {
                int superAdminCount = await _masterContext.AppUsers
                    .CountAsync(u => u.CompanyId == company.Id
                                  && u.RoleId == (int)RoleTypes.SuperAdmin
                                  && !u.IsDeleted);

                if (superAdminCount <= 1)
                    return Result.Fail("Organizasyonda en az bir SuperAdmin kalmalı");
            }

            user.RoleId = role.Id;
            user.UpdateUser = userId;
            user.UpdateDate = DateTime.UtcNow;

            await _masterContext.SaveChangesAsync();

            return Result.Success("Kullanıcı rolü başarıyla değiştirildi");
        }

        public async Task<Result<List<CompanyUserDTO>>> GetCompanyUsersAsync()
        {
            var userId = _currentUser.UserId;
            var companyId = _currentUser.CompanyId;
            var role = _currentUser.Role;

            if (userId == null || companyId == null)
                return Result<List<CompanyUserDTO>>.Unauthorized("Geçersiz Oturum");

            if (role == null || role == RoleTypes.User.ToString())
                return Result<List<CompanyUserDTO>>.Forbidden("Bu işlemi yapamazsınız");

            var company = await _masterContext.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.PublicId == companyId);

            if (company == null)
                return Result<List<CompanyUserDTO>>.NotFound("Organizasyon bulunamadı");

            var users = await _masterContext.AppUsers
                .AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.CompanyId == company.Id)
                .Select(u => new CompanyUserDTO
                (
                    u.PublicId,
                    u.Role.Name,
                    u.Name,
                    u.Email,
                    u.IsVerified,
                    u.CreateDate
                ))
                .ToListAsync();

            return Result<List<CompanyUserDTO>>.Success(users);
        }
    }
}