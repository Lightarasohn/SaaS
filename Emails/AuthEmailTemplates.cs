using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Emails
{
    public static class AuthEmailTemplates
    {
        public static string VerifyAccountSubject {get; }= "Hesap Aktivasyonu";
        public static string VerifyAccountBody(string userPublicId)
        {
            return $"""
                <div>
                <p>
                 Aşağıdaki linke ile hesabınızı aktifleştirin:
                </p>
                <a href="http://localhost:5235/verify-account?publicId={userPublicId}">Hesabı Aktifleştir</a>
                </div>
                """;
        }
    }
}