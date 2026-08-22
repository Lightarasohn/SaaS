using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace CMS.Services
{
    public class RecoveryKeyService
    {
        public static string GenerateRecoveryKey()
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(16);

            string hexString = Convert.ToHexString(randomBytes);

            string keyPart = hexString.Substring(0, 16);

            return $"CMS-{keyPart.Substring(0, 4)}-{keyPart.Substring(4, 4)}-{keyPart.Substring(8, 4)}-{keyPart.Substring(12, 4)}";
        }
    }
}