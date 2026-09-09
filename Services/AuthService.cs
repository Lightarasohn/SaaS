using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using SaaS.DTOs;
using SaaS.Emails;
using SaaS.Interfaces;
using SaaS.Models;
using Microsoft.EntityFrameworkCore;
using SaaS.Database.Contexts.Master;
using SaaS.DTOs.AuthDTOs;
using SaaS.Utils;
using Microsoft.Extensions.Options;

namespace SaaS.Services
{
    public class AuthService : IAuthService
    {

        private const string TOKEN_TYPE_ACTIVATION = "Hesap Aktivasyonu";
        private const string TOKEN_TYPE_CHANGE_PASSWORD = "Parola Yenile";
        private const int PLAN_ID_FREE = 1;
        private const int PASSWORD_CHANGE_COOLDOWN_DAYS = 7;
        private readonly string _frontendBaseUrl;
        private readonly JwtSettings _jwt;
        private readonly MasterContext _context;
        private readonly IEmailQueue _emailQueue;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;
        public AuthService(MasterContext context, IEmailQueue emailQueue, IConfiguration configuration, ITokenService tokenService, IOptions<JwtSettings> jwt, ILogger<AuthService> logger)
        {
            _context = context;
            _emailQueue = emailQueue;
            _tokenService = tokenService;
            _logger = logger;
            _frontendBaseUrl = (configuration["FrontendBaseUrl"] ?? throw new InvalidOperationException("FrontendBaseUrl yapılandırması eksik."))
                .TrimEnd('/');
            _jwt = jwt.Value;
        }

        public async Task<Result<TokenPair>> Login(LoginDTO loginDto, string? ip, string? userAgent)
        {
            AppUser? user = await _context.AppUsers.AsNoTracking().Include(u => u.Company).FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Başarısız giriş denemesi. IP={Ip}", ip);
                return Result<TokenPair>.Unauthorized("E-posta veya parola yanlış");
            }

            if (user.IsVerified == false)
            {
                var accountActivationToken = await _context.UserTokens.AnyAsync(ut =>
                    ut.UserId == user.Id &&
                    ut.TokenType == TOKEN_TYPE_ACTIVATION &&
                    ut.Used == false &&
                    ut.ExpiresAt >= DateTime.UtcNow);

                if (accountActivationToken == false)
                {
                    string newActivationToken = await CreateUserTokenAsync(user.Id, TOKEN_TYPE_ACTIVATION, TimeSpan.FromDays(3));

                    using CancellationTokenSource emailCTS = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                    try
                    {
                        await _emailQueue.EnqueueAsync(new EmailJob
                                                            (user.Email,
                                                            AuthEmailTemplates.VerifyAccountSubject,
                                                            AuthEmailTemplates.VerifyAccountBody(_frontendBaseUrl, newActivationToken),
                                                            true), emailCTS.Token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Aktivasyon e-postası kuyruğa alınamadı. UserId={UserId}", user.Id);

                        await InvalidateActiveTokensAsync(user.Id, TOKEN_TYPE_ACTIVATION);

                        return Result<TokenPair>.Fail("Hesap aktifleştirilmemiş. Hesap aktifleştirme e-postası gönderilirken bir hata oluştu. Lütfen tekrar giriş yapmayı deneyin.", ResultStatus.Error);
                    }
                }

                return Result<TokenPair>.Forbidden("Hesap aktifleştirilmemiş. Önce Hesabınızı aktifleştirin.");
            }

            var tokens = await IssueTokenPairAsync(user, ip, userAgent);
            _logger.LogInformation("Giriş başarılı. UserId={UserId}, IP={Ip}", user.Id, ip);
            return Result<TokenPair>.Success(tokens, "Giriş Başarılı");
        }

        public async Task<Result<string>> RegisterWithCompany(RegisterWithCompanyDTO registerDTO)
        {
            if (await _context.AppUsers.AnyAsync(u => u.Email == registerDTO.Email))
            {
                return Result<string>.Conflict("Bu e-posta kullanılamaz");
            }

            string rawRecoveryKey;
            AppUser newUser;
            string activationToken;

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                Company newCompany = new Company
                {
                    Name = registerDTO.CompanyName,
                    IsActive = true
                };

                await _context.Companies.AddAsync(newCompany);
                await _context.SaveChangesAsync();

                CompanySubscription newCompanySubscription = new CompanySubscription
                {
                    CompanyId = newCompany.Id,
                    PlanId = PLAN_ID_FREE,
                    ExpiresAt = DateTime.MaxValue,
                    IsActive = true
                };

                await _context.CompanySubscriptions.AddAsync(newCompanySubscription);
                await _context.SaveChangesAsync();

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDTO.Password);
                rawRecoveryKey = RecoveryKeyService.GenerateRecoveryKey();
                string hashedRecoveryKey = RecoveryKeyService.Hash(rawRecoveryKey);

                newUser = new AppUser
                {
                    CompanyId = newCompany.Id,
                    RoleId = (int)RoleTypes.SuperAdmin,
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

                activationToken = await CreateUserTokenAsync(newUser.Id, TOKEN_TYPE_ACTIVATION, TimeSpan.FromDays(3));

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Kayıt başarısız, rollback yapıldı");
                return Result<string>.Fail("Kayıt işlemi sırasında sistemsel bir hata oluştu. Değişiklikler geri alınıyor...", ResultStatus.Error);
            }

            using CancellationTokenSource emailCTS = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                await _emailQueue.EnqueueAsync(new EmailJob(
                                                    newUser.Email,
                                                    AuthEmailTemplates.VerifyAccountSubject,
                                                    AuthEmailTemplates.VerifyAccountBody(_frontendBaseUrl, activationToken),
                                                    true),
                                               emailCTS.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kayıt aktivasyon e-postası kuyruğa alınamadı. UserId={UserId}", newUser.Id);

                await InvalidateActiveTokensAsync(newUser.Id, TOKEN_TYPE_ACTIVATION);

                return Result<string>.Success(rawRecoveryKey, "Kayıt işlemi başarılı ancak doğrulama e-postası gönderilirken bir sorun oluştu. Giriş yapmayı deneyin");
            }

            _logger.LogInformation("Kayıt tamamlandı. UserId={UserId}", newUser.Id);
            return Result<string>.Success(rawRecoveryKey, "Başarıyla kayıt olundu. Lütfen giriş yapmadan önce e-postanıza gelen link ile kaydınızı tamamlayınız.");
        }

        public async Task<Result<string>> RegisterToCompany(RegisterToCompanyDTO registerDTO)
        {
            if (await _context.AppUsers.AnyAsync(u => u.Email == registerDTO.Email))
            {
                return Result<string>.Conflict("Bu e-posta kullanılamaz");
            }

            if (!Guid.TryParse(registerDTO.InviteCode, out Guid companyPublicId))
            {
                return Result<string>.Fail("Geçersiz davet kodu formatı.");
            }

            var targetCompany = await _context.Companies
                .FirstOrDefaultAsync(c => c.PublicId == companyPublicId && c.IsActive);

            if (targetCompany == null)
            {
                return Result<string>.NotFound("Şirket bulunamadı veya organizasyon şu anda pasif durumda.");
            }

            string activationToken;

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDTO.Password);
            string rawRecoveryKey = RecoveryKeyService.GenerateRecoveryKey();
            string hashedRecoveryKey = RecoveryKeyService.Hash(rawRecoveryKey);

            AppUser newUser = new AppUser
            {
                CompanyId = targetCompany.Id,
                RoleId = (int)RoleTypes.User,
                DistributorId = null,
                Email = registerDTO.Email,
                Name = registerDTO.Name,
                PasswordHash = hashedPassword,
                RecoveryKeyHash = hashedRecoveryKey
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.AppUsers.AddAsync(newUser);
                await _context.SaveChangesAsync();
                newUser.CreateUser = newUser.Id;
                await _context.SaveChangesAsync();
                activationToken = await CreateUserTokenAsync(newUser.Id, TOKEN_TYPE_ACTIVATION, TimeSpan.FromDays(3));

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Kayıt başarısız, rollback yapıldı");
                return Result<string>.Fail("Kayıt işlemi sırasında sistemsel bir hata oluştu. Değişiklikler geri alınıyor...", ResultStatus.Error);
            }

            using CancellationTokenSource emailCTS = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                await _emailQueue.EnqueueAsync(new EmailJob(
                                                    newUser.Email,
                                                    AuthEmailTemplates.VerifyAccountSubject,
                                                    AuthEmailTemplates.VerifyAccountBody(_frontendBaseUrl, activationToken),
                                                    true),
                                               emailCTS.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kayıt aktivasyon e-postası kuyruğa alınamadı. UserId={UserId}", newUser.Id);
                await InvalidateActiveTokensAsync(newUser.Id, TOKEN_TYPE_ACTIVATION);

                return Result<string>.Success(rawRecoveryKey, "Kayıt işlemi başarılı ancak doğrulama e-postası gönderilirken bir sorun oluştu.");
            }
            _logger.LogInformation("Kayıt tamamlandı. UserId={UserId}", newUser.Id);
            return Result<string>.Success(rawRecoveryKey, "Başarıyla kayıt olundu.\nLütfen giriş yapmadan önce e-postanıza gelen link ile kaydınızı tamamlayınız.");
        }

        public async Task<Result> VerifyAccount(string rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                return Result.NotFound("Aktivasyon linki geçersiz");

            string hash = SecureTokenService.Hash(rawToken);

            UserToken? token = await _context.UserTokens
                .Include(ut => ut.User)
                .FirstOrDefaultAsync(ut => ut.TokenHash == hash && ut.TokenType == TOKEN_TYPE_ACTIVATION);

            if (token == null)
                return Result.NotFound("Aktivasyon linki geçersiz");

            if (token.User.IsVerified)
                return Result.Conflict("Hesap zaten aktifleştirilmiş");

            if (token.Used || token.ExpiresAt <= DateTime.UtcNow)
                return Result.NotFound("Aktivasyon linki geçersiz veya süresi dolmuş");

            token.User.IsVerified = true;
            token.Used = true;
            await _context.SaveChangesAsync();
            return Result.Success("Hesap aktifleştirildi");
        }

        public async Task<Result> ForgotPassword(ForgotPasswordDTO forgotPasswordDTO)
        {
            _logger.LogDebug("Parola sıfırlama isteği alındı");
            // Kullanıcı var/yok fark etmeksizin aynı cevap (enumeration koruması)
            const string GENERIC_MESSAGE = "E-postanıza parola yenileme linki gönderilmiştir.";

            AppUser? user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Email == forgotPasswordDTO.Email);

            if (user == null)
                return Result.Success(GENERIC_MESSAGE);

            string passwordToken = await CreateUserTokenAsync(
                user.Id, TOKEN_TYPE_CHANGE_PASSWORD, TimeSpan.FromMinutes(30));

            using var emailCTS = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                await _emailQueue.EnqueueAsync(new EmailJob(
                    user.Email,
                    AuthEmailTemplates.ForgotPasswordSubject,
                    AuthEmailTemplates.ForgotPasswordBody(_frontendBaseUrl, passwordToken),
                    true), emailCTS.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Parola sıfırlama e-postası kuyruğa alınamadı. UserId={UserId}", user.Id);
                await InvalidateActiveTokensAsync(user.Id, TOKEN_TYPE_CHANGE_PASSWORD);
            }
            _logger.LogInformation("Parola sıfırlama isteği işlendi. UserId={UserId}", user.Id);
            return Result.Success(GENERIC_MESSAGE);
        }

        public async Task<Result> ChangePassword(string rawToken, ChangePasswordDTO newPasswordDTO)
        {
            _logger.LogDebug("Parola değiştirme isteği alındı");
            UserToken? token = await GetValidTokenAsync(rawToken, TOKEN_TYPE_CHANGE_PASSWORD);

            if (token == null)
                return Result.Fail("Parola yenileme linki geçersiz veya süresi dolmuş");

            if (IsInPasswordCooldown(token.User))
                return Result.Conflict("Parolanızı kısa bir süre önce yenilediğinizden dolayı yenileyemezsiniz");

            if (BCrypt.Net.BCrypt.Verify(newPasswordDTO.NewPassword, token.User.PasswordHash))
                return Result.Fail("Yeni parola eski parola ile aynı olamaz");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                token.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPasswordDTO.NewPassword);
                token.User.PasswordChangedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await InvalidateActiveTokensAsync(token.UserId, TOKEN_TYPE_CHANGE_PASSWORD);
                await RevokeAllUserTokensAsync(token.UserId);
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Parola yenilenemedi, rollback yapıldı. UserId={UserId}", token.UserId);
                return Result.Fail("Parola yenilenirken sistemsel bir hata oluştu.", ResultStatus.Error);
            }
            _logger.LogInformation("Parola başarıyla yenilendi. UserId={UserId}", token.UserId);
            return Result.Success("Parolanız yenilenmiştir");
        }

        public async Task<Result> ValidateChangePassword(string rawToken)
        {
            _logger.LogDebug("Parola yenileme doğrulaması isteği alındı");
            UserToken? token = await GetValidTokenAsync(rawToken, TOKEN_TYPE_CHANGE_PASSWORD);

            if (token == null)
                return Result.Fail("Parola yenileme linki geçersiz veya süresi dolmuş");

            if (IsInPasswordCooldown(token.User))
                return Result.Conflict("Parolanızı kısa bir süre önce yenilediğinizden dolayı yenileyemezsiniz");

            _logger.LogDebug("Parola yenileme doğrulaması başarılı. UserId={UserId}", token.UserId);
            return Result.Success("Parola yenilenebilir");
        }

        public async Task<Result<TokenPair>> RefreshAsync(string rawRefreshToken, string? ip, string? userAgent)
        {
            _logger.LogDebug("Oturum yenileme isteği alındı");
            if (string.IsNullOrWhiteSpace(rawRefreshToken))
                return Result<TokenPair>.Unauthorized("Geçersiz oturum");

            string hash = SecureTokenService.Hash(rawRefreshToken);

            RefreshToken? stored = await _context.RefreshTokens
                .Include(rt => rt.User)
                .ThenInclude(u => u.Company)
                .FirstOrDefaultAsync(rt => rt.TokenHash == hash);

            if (stored == null)
                return Result<TokenPair>.Unauthorized("Geçersiz oturum");

            // REUSE DETECTION: iptal edilmiş token tekrar sunuldu → sızıntı varsayımı
            if (stored.RevokedAt != null)
            {
                await RevokeAllUserTokensAsync(stored.UserId);
                _logger.LogWarning(
                    "Refresh token yeniden kullanıldı, tüm oturumlar iptal edildi. UserId={UserId}, IP={Ip}",
                    stored.UserId, ip);
                return Result<TokenPair>.Unauthorized("Oturum güvenlik nedeniyle sonlandırıldı. Lütfen tekrar giriş yapın.");
            }

            if (stored.ExpiresAt <= DateTime.UtcNow)
                return Result<TokenPair>.Unauthorized("Oturum süresi doldu. Lütfen tekrar giriş yapın.");

            if (!stored.User.IsVerified)
                return Result<TokenPair>.Forbidden("Hesap aktifleştirilmemiş.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var newPair = await IssueTokenPairAsync(stored.User, ip, userAgent);

                // Eskisini yak ve zinciri kur
                string newHash = SecureTokenService.Hash(newPair.RefreshToken);
                int newId = await _context.RefreshTokens
                    .Where(rt => rt.TokenHash == newHash)
                    .Select(rt => rt.Id)
                    .FirstAsync();

                stored.RevokedAt = DateTime.UtcNow;
                stored.ReplacedByTokenId = newId;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("Oturum başarıyla yenilendi. UserId={UserId}, IP={Ip}", stored.UserId, ip);
                return Result<TokenPair>.Success(newPair, "Token yenilendi");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Oturum yenilenemedi, rollback yapıldı. UserId={UserId}", stored.UserId);
                return Result<TokenPair>.Fail("Oturum yenilenirken bir hata oluştu.", ResultStatus.Error);
            }
        }

        public async Task RevokeRefreshTokenAsync(string rawRefreshToken)
        {
            var hash = SecureTokenService.Hash(rawRefreshToken);
            int affected = await _context.RefreshTokens
                .Where(rt => rt.TokenHash == hash && rt.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevokedAt, DateTime.UtcNow));
            _logger.LogInformation("Çıkış yapıldı. İptal edilen token sayısı={Count}", affected);
        }

        public async Task<Result<MeDTO>> GetMe(string publicId)
        {
            _logger.LogDebug("Kullanıcı bilgisi isteniyor");
            if (!Guid.TryParse(publicId, out Guid userPublicId))
                return Result<MeDTO>.Unauthorized("Geçersiz oturum");

            var userDto = await _context.AppUsers
                                    .AsNoTracking()
                                    .Select(u => new MeDTO
                                    {
                                        PublicId = u.PublicId,
                                        Name = u.Name,
                                        Email = u.Email,
                                        IsVerified = u.IsVerified,
                                        PasswordChangedAt = u.PasswordChangedAt,
                                        CreateDate = u.CreateDate,
                                        CompanyName = u.Company.Name,
                                        RoleName = u.Role.Name
                                    })
                                    .FirstOrDefaultAsync(u => u.PublicId == userPublicId);

            if (userDto == null)
                return Result<MeDTO>.NotFound("Kullanıcı bulunamadı");
            _logger.LogDebug("Kullanıcı bilgisi başarıyla getirildi. PublicId={PublicId}", userDto.PublicId);
            return Result<MeDTO>.Success(userDto);

        }

        public async Task<Result> ValidateVerifyAccount(string rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                return Result.NotFound("Aktivasyon linki geçersiz");

            string hash = SecureTokenService.Hash(rawToken);

            UserToken? token = await _context.UserTokens
                .Include(ut => ut.User)
                .FirstOrDefaultAsync(ut => ut.TokenHash == hash && ut.TokenType == TOKEN_TYPE_ACTIVATION);

            if (token == null)
                return Result.NotFound("Aktivasyon linki geçersiz");

            if (token.User.IsVerified)
                return Result.Conflict("Hesap zaten aktifleştirilmiş");

            if (token.Used || token.ExpiresAt <= DateTime.UtcNow)
                return Result.NotFound("Aktivasyon linki geçersiz veya süresi dolmuş");

            return Result.Success("Hesap aktifleştirilebilir");
        }

        private async Task RevokeAllUserTokensAsync(int userId)
        {
            await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevokedAt, DateTime.UtcNow));
        }

        private async Task<TokenPair> IssueTokenPairAsync(AppUser user, string? ip, string? userAgent)
        {
            var (accessToken, accessTokenExpiresAt) = _tokenService.CreateAccessToken(user);
            var (rawRefresh, refreshHash) = _tokenService.CreateRefreshToken();

            var refreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenDays);

            await _context.RefreshTokens.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshHash,
                ExpiresAt = refreshTokenExpiresAt.UtcDateTime,
                CreateDate = DateTime.UtcNow,
                CreatedByIp = ip,
                UserAgent = userAgent
            });
            await _context.SaveChangesAsync();

            return new TokenPair(accessToken, accessTokenExpiresAt, rawRefresh, refreshTokenExpiresAt);
        }

        private static bool IsInPasswordCooldown(AppUser user)
        {
            return user.PasswordChangedAt is DateTime changedAt
               && changedAt.AddDays(PASSWORD_CHANGE_COOLDOWN_DAYS) > DateTime.UtcNow;
        }

        private async Task<UserToken?> GetValidTokenAsync(string rawToken, string tokenType)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                return null;

            string hash = SecureTokenService.Hash(rawToken);

            UserToken? token = await _context.UserTokens
                .Include(ut => ut.User)
                .FirstOrDefaultAsync(ut => ut.TokenHash == hash && ut.TokenType == tokenType);

            if (token == null || token.Used || token.ExpiresAt <= DateTime.UtcNow)
                return null;

            return token;
        }

        private async Task<string> CreateUserTokenAsync(int userId, string tokenType, TimeSpan lifeTime)
        {
            await InvalidateActiveTokensAsync(userId, tokenType);

            var (raw, hash) = SecureTokenService.GenerateSecureToken();

            UserToken userToken = new UserToken
            {
                UserId = userId,
                TokenHash = hash,
                TokenType = tokenType,
                ExpiresAt = DateTime.UtcNow.Add(lifeTime),
                Used = false,
                CreateDate = DateTime.UtcNow
            };

            await _context.UserTokens.AddAsync(userToken);
            await _context.SaveChangesAsync();
            return raw;
        }

        private async Task InvalidateActiveTokensAsync(int userId, string tokenType)
        {
            await _context.UserTokens
                                .Where(ut => ut.UserId == userId && ut.TokenType == tokenType && !ut.Used)
                                .ExecuteUpdateAsync(s => s.SetProperty(ut => ut.Used, true));
        }
    }

}