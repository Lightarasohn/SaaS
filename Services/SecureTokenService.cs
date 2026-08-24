using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SaaS.Services
{
    public static class SecureTokenService
    {
        public static (string RawToken, string TokenHash) GenerateSecureToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);

            string raw = Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');

            return (raw, Hash(raw));
        }

        public static string Hash(string rawToken)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        }
    }
}