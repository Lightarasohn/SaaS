using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaS.Emails
{
    public static class AuthEmailTemplates
    {
        public static string ForgotPasswordSubject {get; } = "Parola Yenileme";
        public static string VerifyAccountSubject {get; }= "Hesap Aktivasyonu";
        // TODO: FRONTEND HESABI AKTIİFLEŞTİR SAYFASI OLMALI
        public static string VerifyAccountBody(string baseUrl, string activationToken)
        {
            string encodedToken = Uri.EscapeDataString(activationToken);
            return $"""
                <div>
                <p>
                 Aşağıdaki link ile hesabınızı aktifleştirin (3 gün geçerlidir):
                </p>
                <a href="{baseUrl}/verify-account?rawToken={encodedToken}">Hesabı Aktifleştir</a>
                </div>
                """;
        }
        // TODO: FRONTEND PAROLA YENİLE SAYFASI OLMALI
        public static string ForgotPasswordBody(string baseUrl, string changePasswordToken)
        {
            string encodedToken = Uri.EscapeDataString(changePasswordToken);
            return $"""
                    <div>
                        <p>
                            Aşağıdaki link ile parolanızı yenileyiniz:
                        </p>
                        <a href="{baseUrl}/change-password?token={encodedToken}">Parola Yenile</a>
                    </div>
                    """;
        }
    }
}