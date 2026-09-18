using System;
using System.Globalization;

namespace SaaS.Emails
{
    public static class SubscriptionEmailTemplates
    {
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");

        private static string FormatDate(DateTime utc)
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
                var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tz);
                return local.ToString("dd MMMM yyyy HH:mm", Tr);
            }
            catch (TimeZoneNotFoundException)
            {
                return utc.ToString("dd MMMM yyyy HH:mm", Tr) + " (UTC)";
            }
        }

        private static string Wrap(string title, string innerHtml) => $@"
<div style=""font-family:Segoe UI,Arial,sans-serif;max-width:600px;margin:0 auto;padding:24px;color:#1f2933;"">
  <h2 style=""margin:0 0 16px;font-size:20px;"">{title}</h2>
  {innerHtml}
  <hr style=""border:none;border-top:1px solid #e4e7eb;margin:24px 0;"" />
  <p style=""font-size:12px;color:#7b8794;margin:0;"">Bu e-posta hesap sahibi olarak size gönderilmiştir. Otomatik bir iletidir, lütfen yanıtlamayınız.</p>
</div>";

        private static string Greeting(string ownerName, string companyName) =>
            $"<p>Merhaba <strong>{ownerName}</strong>,</p><p style=\"color:#52606d;font-size:14px;margin-top:-8px;\">{companyName} hesabı hakkında bir bilgilendirme.</p>";

        public const string SubscriptionStartedSubject = "Aboneliğiniz başlatıldı";

        public static string SubscriptionStartedBody(string ownerName, string companyName, string planName, DateTime expiresAtUtc, bool isFreePlan, bool autoRenew)
        {
            var body = isFreePlan
                ? "<p><strong>Ücretsiz</strong> planınız süresiz olarak aktiftir.</p>"
                : $@"<p>Aboneliğiniz <strong>{FormatDate(expiresAtUtc)}</strong> tarihine kadar geçerlidir.</p>
                     <p>{(autoRenew
                          ? "Dönem sonunda aboneliğiniz otomatik olarak yenilenecektir."
                          : "Otomatik yenileme kapalı. Dönem sonunda hesabınız ücretsiz plana geçecektir.")}</p>";

            return Wrap("Aboneliğiniz aktif", $@"
{Greeting(ownerName, companyName)}
<p><strong>{planName}</strong> planına geçişiniz başarıyla tamamlandı.</p>
{body}");
        }

        public const string SubscriptionRenewedSubject = "Aboneliğiniz uzatıldı";

        public static string SubscriptionRenewedBody(string ownerName, string companyName, string planName, DateTime newExpiresAtUtc, int monthsAdded) =>
            Wrap("Abonelik süreniz uzatıldı", $@"
{Greeting(ownerName, companyName)}
<p><strong>{planName}</strong> planınıza <strong>{monthsAdded} ay</strong> eklendi.</p>
<p>Yeni bitiş tarihiniz: <strong>{FormatDate(newExpiresAtUtc)}</strong></p>");

        public const string SubscriptionCanceledSubject = "Otomatik yenileme kapatıldı";

        public static string SubscriptionCanceledBody(string ownerName, string companyName, string planName, DateTime expiresAtUtc) =>
            Wrap("Otomatik yenileme kapatıldı", $@"
{Greeting(ownerName, companyName)}
<p><strong>{planName}</strong> planınızın otomatik yenilenmesi iptal edildi.</p>
<p>Hizmetiniz <strong>{FormatDate(expiresAtUtc)}</strong> tarihine kadar kesintisiz devam edecek, bu tarihten sonra hesabınız otomatik olarak <strong>Ücretsiz</strong> plana geçecektir.</p>
<p>Fikrinizi değiştirirseniz dönem sona ermeden otomatik yenilemeyi tekrar açabilirsiniz.</p>");

        public const string AutoRenewEnabledSubject = "Otomatik yenileme tekrar açıldı";

        public static string AutoRenewEnabledBody(string ownerName, string companyName, string planName, DateTime expiresAtUtc) =>
            Wrap("Otomatik yenileme açık", $@"
{Greeting(ownerName, companyName)}
<p><strong>{planName}</strong> planınız için otomatik yenileme tekrar etkinleştirildi.</p>
<p>Aboneliğiniz <strong>{FormatDate(expiresAtUtc)}</strong> tarihinde otomatik olarak yenilenecektir.</p>");

        public const string AutoRenewSucceededSubject = "Aboneliğiniz otomatik olarak yenilendi";

        public static string AutoRenewSucceededBody(string ownerName, string companyName, string planName, DateTime newExpiresAtUtc) =>
            Wrap("Aboneliğiniz yenilendi", $@"
{Greeting(ownerName, companyName)}
<p><strong>{planName}</strong> planınız otomatik olarak yenilendi.</p>
<p>Yeni bitiş tarihiniz: <strong>{FormatDate(newExpiresAtUtc)}</strong></p>
<p>Otomatik yenilemeyi istediğiniz zaman panelinizden kapatabilirsiniz.</p>");

        public const string RenewalFailedSubject = "Abonelik yenilemesi başarısız — işlem gerekli";

        public static string RenewalFailedBody(string ownerName, string companyName, string planName, DateTime graceEndsAtUtc, int attempt, int maxAttempts) =>
            Wrap("Ödeme alınamadı", $@"
{Greeting(ownerName, companyName)}
<p><strong>{planName}</strong> planınızın otomatik yenilenmesi sırasında ödeme alınamadı. (Deneme {attempt}/{maxAttempts})</p>
<p>Hizmetiniz kesintiye uğramaması için <strong>{FormatDate(graceEndsAtUtc)}</strong> tarihine kadar açık kalacak.</p>
<p>Lütfen bu tarihe kadar ödeme yönteminizi güncelleyin; aksi halde hesabınız ücretsiz plana geçirilecektir.</p>");

        public const string SubscriptionExpiredSubject = "Aboneliğiniz sona erdi";

        public static string SubscriptionExpiredBody(string ownerName, string companyName, string oldPlanName, DateTime expiredAtUtc, bool afterFailedPayment) =>
            Wrap("Aboneliğiniz sona erdi", $@"
{Greeting(ownerName, companyName)}
<p><strong>{oldPlanName}</strong> planınızın süresi <strong>{FormatDate(expiredAtUtc)}</strong> tarihinde doldu ve hesabınız <strong>Ücretsiz</strong> plana geçirildi.</p>
{(afterFailedPayment ? "<p>Tekrarlanan ödeme denemeleri başarısız olduğu için yenileme tamamlanamadı.</p>" : "")}
<p>Ücretli plana özel modüllere erişiminiz durduruldu; <strong>verileriniz korunmaktadır.</strong></p>
<p>Kaldığınız yerden devam etmek için panelinizden yeni bir plan seçebilirsiniz.</p>");
    }
}