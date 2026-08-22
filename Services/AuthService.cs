using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CMS.DTOs;
using CMS.Emails;
using CMS.Interfaces;
using CMS.Models;
using Microsoft.EntityFrameworkCore;
using CMS.Database.Contexts.Master;
using CMS.DTOs.AuthDTOs;

namespace CMS.Services
{
    public class AuthService : IAuthService
    {
        private const int DEFAULT_USER_ROLE_ID = 1;
        private const int DEFAULT_ADMIN_ROLE_ID = 2;
        private const int DEFAULT_SUPERADMIN_ROLE_ID = 3;
        private readonly MasterContext _context;
        private readonly IEmailService _emailService;
        public AuthService(MasterContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<Result<AppUser>> Login(LoginDTO loginDto)
        {
            AppUser? user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null)
            {
                return Result<AppUser>.Fail("E-posta veya parola yanlış");
            }

            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return Result<AppUser>.Fail("E-posta veya parola yanlış");
            }

            if (user.IsVerified == false)
            {
                var accountActivationToken = await _context.UserTokens.AnyAsync(ut => 
                    ut.UserId == user.Id &&
                    ut.TokenType == "Hesap Aktivasyonu" &&
                    ut.Used == false &&
                    DateTime.Compare(DateTime.UtcNow, ut.ExpiresAt) <= 0);

                if (accountActivationToken == false)
                {
                    CancellationTokenSource emailCTS = new CancellationTokenSource();
                    try
                    {
                        await _emailService.SendEmailAsync(
                            user.Email,
                            AuthEmailTemplates.VerifyAccountSubject,
                            AuthEmailTemplates.VerifyAccountBody(user.PublicId.ToString()),
                            emailCTS.Token,
                            true);
                    }
                    catch(Exception ex)
                    {
                        emailCTS.Cancel();
                        Console.WriteLine($"SMTP/Email Hatası: {ex.Message}");
                        return Result<AppUser>.Success("Hesabınız aktifleştirilmemişti fakat yeni doğrulama e-postası gönderilirken bir sorun oluştu.");
                    }
                }

                return Result<AppUser>.Fail("Hesap aktifleştirilmemiş. Önce Hesabınızı aktifleştirin.");
            }

            return Result<AppUser>.Success(user, "Giriş Başarılı");
        }

        public async Task<Result<string>> RegisterWithCompany(RegisterWithCompanyDTO registerDTO)
        {
            if (await _context.AppUsers.AnyAsync(u => u.Email == registerDTO.Email))
            {
                return Result<string>.Fail("Bu e-posta kullanılamaz");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            CancellationTokenSource emailCTS = new CancellationTokenSource();
            string rawRecoveryKey;
            AppUser newUser;

            try
            {
                Company newCompany = new Company
                {
                    Name = registerDTO.CompanyName,
                    IsActive = true
                };

                await _context.Companies.AddAsync(newCompany);
                await _context.SaveChangesAsync();

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDTO.Password);
                rawRecoveryKey = RecoveryKeyService.GenerateRecoveryKey();
                string hashedRecoveryKey = BCrypt.Net.BCrypt.HashPassword(rawRecoveryKey);

                newUser = new AppUser
                {
                    CompanyId = newCompany.Id,
                    RoleId = DEFAULT_SUPERADMIN_ROLE_ID,
                    DistributorId = null,
                    Email = registerDTO.Email,
                    Name = registerDTO.Name,
                    PasswordHash = hashedPassword,
                    RecoveryKeyHash = hashedRecoveryKey
                };

                await _context.AppUsers.AddAsync(newUser);
                await _context.SaveChangesAsync();
                newUser.CreateUser = newUser.Id;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();

                return Result<string>.Fail("Kayıt işlemi sırasında sistemsel bir hata oluştu. Değişiklikler geri alınıyor...");
            }

            try
            {
                await _emailService.SendEmailAsync(
                    newUser.Email,
                    AuthEmailTemplates.VerifyAccountSubject,
                    AuthEmailTemplates.VerifyAccountBody(newUser.PublicId.ToString()),
                    emailCTS.Token,
                    true);
            }
            catch (Exception ex)
            {
                emailCTS.Cancel();
                Console.WriteLine($"SMTP/Email Hatası: {ex.Message}");

                return Result<string>.Success(rawRecoveryKey, "Kayıt işlemi başarılı ancak doğrulama e-postası gönderilirken bir sorun oluştu.");
            }

            return Result<string>.Success(rawRecoveryKey, "Başarıyla kayıt olundu.Lütfen giriş yapmadan önce e-postanıza gelen link ile kaydınızı tamamlayınız.");
        }

        public async Task<Result<string>> RegisterToCompany(RegisterToCompanyDTO registerDTO)
        {
            if (await _context.AppUsers.AnyAsync(u => u.Email == registerDTO.Email))
            {
                return Result<string>.Fail("Bu e-posta kullanılamaz");
            }

            if (!Guid.TryParse(registerDTO.InviteCode, out Guid companyPublicId))
            {
                return Result<string>.Fail("Geçersiz davet kodu formatı.");
            }

            var targetCompany = await _context.Companies
                .FirstOrDefaultAsync(c => c.PublicId == companyPublicId && c.IsActive);

            if (targetCompany == null)
            {
                return Result<string>.Fail("Şirket bulunamadı veya organizasyon şu anda pasif durumda.");
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDTO.Password);
            string rawRecoveryKey = RecoveryKeyService.GenerateRecoveryKey();
            string hashedRecoveryKey = BCrypt.Net.BCrypt.HashPassword(rawRecoveryKey);

            AppUser newUser = new AppUser
            {
                CompanyId = targetCompany.Id,
                RoleId = DEFAULT_USER_ROLE_ID,
                DistributorId = null,
                Email = registerDTO.Email,
                Name = registerDTO.Name,
                PasswordHash = hashedPassword,
                RecoveryKeyHash = hashedRecoveryKey
            };

            await _context.AppUsers.AddAsync(newUser);
            await _context.SaveChangesAsync();
            newUser.CreateUser = newUser.Id;
            await _context.SaveChangesAsync();

            CancellationTokenSource emailCTS = new CancellationTokenSource();
            try
            {
                await _emailService.SendEmailAsync(
                    newUser.Email,
                    AuthEmailTemplates.VerifyAccountSubject,
                    AuthEmailTemplates.VerifyAccountBody(newUser.PublicId.ToString()),
                    emailCTS.Token,
                    true);
            }
            catch (Exception ex)
            {
                emailCTS.Cancel();
                Console.WriteLine($"SMTP/Email Hatası: {ex.Message}");

                return Result<string>.Success(rawRecoveryKey, "Kayıt işlemi başarılı ancak doğrulama e-postası gönderilirken bir sorun oluştu.");
            }

            return Result<string>.Success(rawRecoveryKey, "Başarıyla kayıt olundu.\nLütfen giriş yapmadan önce e-postanıza gelen link ile kaydınızı tamamlayınız.");
        }

        public async Task<Result<string>> VerifyAccount(string userPublicId)
        {
            var userGuidId = Guid.Parse(userPublicId);
            AppUser? user = await _context.AppUsers.FirstOrDefaultAsync(u => u.PublicId == userGuidId);

            if (user == null)
            {
                return Result<string>.Fail("Hesap aktifleştirilirken sorun oluştu");
            }

            if (user.IsVerified)
            {
                return Result<string>.Success("Hesap zaten aktifleştirilmiş");
            }

            user.IsVerified = true;
            await _context.SaveChangesAsync();

            return Result<string>.Success("Hesap aktifleştirildi");
        }
    }
}