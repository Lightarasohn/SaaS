using System;
using System.Security.Cryptography;
using System.Text;

namespace SaaS.Services
{
    public static class RecoveryKeyService
    {
        private const string PREFIX = "SaaS";
        private const int ENTROPY_BYTES = 15;   // 120 bit -> tam 24 karakter, padding yok
        private const int GROUP_SIZE = 4;

        // Crockford Base32: I, L, O, U yok (okuma/yazma hatalarını önler)
        private const string ALPHABET = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        /// <summary>Kullanıcıya gösterilecek kurtarma anahtarı: SaaS-XXXX-XXXX-XXXX-XXXX-XXXX-XXXX</summary>
        public static string GenerateRecoveryKey()
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(ENTROPY_BYTES);
            string encoded = ToBase32(randomBytes);

            StringBuilder sb = new StringBuilder(PREFIX);
            for (int i = 0; i < encoded.Length; i += GROUP_SIZE)
            {
                sb.Append('-').Append(encoded, i, GROUP_SIZE);
            }

            return sb.ToString();
        }

        public static string Hash(string recoveryKey)
            => BCrypt.Net.BCrypt.HashPassword(Normalize(recoveryKey));

        public static bool Verify(string? userInput, string storedHash)
        {
            string normalized = Normalize(userInput);
            if (normalized.Length == 0) return false;

            return BCrypt.Net.BCrypt.Verify(normalized, storedHash);
        }

        /// <summary>
        /// Kullanıcı girdisini kanonik forma çevirir: prefix ve tireler atılır,
        /// büyük harfe çevrilir, karışan karakterler eşlenir (O->0, I/L->1, U->V).
        /// </summary>
        private static string Normalize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            string s = input.Trim().ToUpperInvariant();

            // Prefix'teki harfler alfabede de var, önce açıkça temizle
            if (s.StartsWith(PREFIX.ToUpperInvariant(), StringComparison.Ordinal))
                s = s.Substring(PREFIX.Length);

            StringBuilder sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                char mapped = c switch
                {
                    'O' => '0',
                    'I' or 'L' => '1',
                    'U' => 'V',
                    _ => c
                };

                if (ALPHABET.IndexOf(mapped) >= 0) sb.Append(mapped);
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