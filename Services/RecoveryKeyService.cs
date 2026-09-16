using System;
using System.Security.Cryptography;
using System.Text;

namespace SaaS.Services
{
    public static class RecoveryKeyService
    {
        private const string KEY_PREFIX = "SaaS";
        private const string HASH_PREFIX = "SaaSH1";   // H1 = hash algoritması v1, ileride değişirse ayırt edilir
        private const int ENTROPY_BYTES = 15;           // ham anahtar: 120 bit -> 24 karakter
        private const int GROUP_SIZE = 4;

        // Crockford Base32: I, L, O, U yok (okuma/yazma hatalarını önler)
        private const string ALPHABET = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        /// <summary>Kullanıcıya gösterilecek kurtarma anahtarı: SaaS-XXXX-XXXX-XXXX-XXXX-XXXX-XXXX</summary>
        public static string GenerateRecoveryKey()
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(ENTROPY_BYTES);
            return Format(KEY_PREFIX, ToBase32(randomBytes));
        }

        /// <summary>DB'de saklanan hash: SaaSH1-XXXX-XXXX-XXXX-XXXX-XXXX-XXXX-XXXX-XXXX-XXXX-XXXX-XXXX-XXXX-XX</summary>
        public static string Hash(string recoveryKey)
        {
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(Normalize(recoveryKey)));
            return Format(HASH_PREFIX, ToBase32(hashBytes)); // 256 bit -> 52 karakter
        }

        public static bool Verify(string? userInput, string storedHash)
        {
            string normalized = Normalize(userInput);
            if (normalized.Length == 0) return false;

            string computedHash = Hash(normalized);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHash),
                Encoding.UTF8.GetBytes(storedHash));
        }

        private static string Format(string prefix, string base32Body)
        {
            StringBuilder sb = new StringBuilder(prefix);
            for (int i = 0; i < base32Body.Length; i += GROUP_SIZE)
            {
                int len = Math.Min(GROUP_SIZE, base32Body.Length - i);
                sb.Append('-').Append(base32Body, i, len);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Kullanıcı girdisini kanonik forma çevirir: prefix ve tireler atılır,
        /// büyük harfe çevrilir, karışan karakterler eşlenir (O->0, I/L->1, U->V).
        /// </summary>
        private static string Normalize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            string s = input.Trim().ToUpperInvariant();

            if (s.StartsWith(KEY_PREFIX.ToUpperInvariant(), StringComparison.Ordinal))
                s = s.Substring(KEY_PREFIX.Length);

            StringBuilder sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                char mapped = c switch
                {
                    'O' => '0',
                    'I' or 'L' => '1',
                    'U' => 'V',
                    '-' => '\0', // tireleri at
                    _ => c
                };

                if (mapped != '\0' && ALPHABET.IndexOf(mapped) >= 0) sb.Append(mapped);
            }

            return sb.ToString();
        }

        private static string ToBase32(byte[] data)
        {
            StringBuilder sb = new StringBuilder((data.Length * 8 + 4) / 5);
            int buffer = 0;
            int bitsLeft = 0;

            foreach (byte b in data)
            {
                buffer = (buffer << 8) | b;
                bitsLeft += 8;

                while (bitsLeft >= 5)
                {
                    bitsLeft -= 5;
                    sb.Append(ALPHABET[(buffer >> bitsLeft) & 31]);
                }
            }

            if (bitsLeft > 0)
                sb.Append(ALPHABET[(buffer << (5 - bitsLeft)) & 31]);

            return sb.ToString();
        }
    }
}