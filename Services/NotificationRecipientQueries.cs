using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SaaS.Database.Contexts.Master;
using SaaS.Utils;

namespace SaaS.Services
{
    public sealed record NotificationRecipient(
        int CompanyId,
        string CompanyName,
        string OwnerName,
        string Email);

    public static class NotificationRecipientQueries
    {
        // Şirketin SuperAdmin/owner kullanıcısını bulur.
        // Birden fazla varsa en eski (ilk kurulan) hesap sahibi kabul edilir.
        public static Task<NotificationRecipient?> GetOwnerRecipientAsync(
            this MasterContext context, int companyId, CancellationToken ct = default)
        {
            return context.AppUsers
                .Where(u => u.CompanyId == companyId
                         && u.RoleId == (int)RoleTypes.SuperAdmin
                         && !u.IsDeleted)
                .OrderBy(u => u.Id)
                .Select(u => new NotificationRecipient(u.CompanyId, u.Company.Name, u.Name, u.Email))
                .FirstOrDefaultAsync(ct);
        }

        // Background servis için toplu sürüm (N+1 sorgu olmasın diye)
        public static async Task<Dictionary<int, NotificationRecipient>> GetOwnerRecipientsAsync(
            this MasterContext context, IReadOnlyCollection<int> companyIds, CancellationToken ct = default)
        {
            var owners = await context.AppUsers
                .Where(u => companyIds.Contains(u.CompanyId)
                         && u.RoleId == (int)RoleTypes.SuperAdmin
                         && !u.IsDeleted)
                .OrderBy(u => u.Id)
                .Select(u => new { u.Id, Recipient = new NotificationRecipient(u.CompanyId, u.Company.Name, u.Name, u.Email) })
                .ToListAsync(ct);

            return owners
                .GroupBy(x => x.Recipient.CompanyId)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).First().Recipient);
        }
    }
}