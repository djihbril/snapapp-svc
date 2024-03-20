using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace SnapApp.Svc.Extensions
{
    public static partial class StringExtensions
    {
        [GeneratedRegex(@"([A-Z])", RegexOptions.Compiled)]
        private static partial Regex CapsRegex();

        private static string Caesar(this string input, short shift)
        {
            int maxChar = Convert.ToInt32(char.MaxValue);
            int minChar = Convert.ToInt32(char.MinValue);
            char[] buffer = input.ToCharArray();

            for (int i = 0; i < buffer.Length; i++)
            {
                int shifted = Convert.ToInt32(buffer[i]) + shift;

                if (shifted > maxChar)
                {
                    shifted -= maxChar;
                }
                else if (shifted < minChar)
                {
                    shifted += maxChar;
                }

                buffer[i] = Convert.ToChar(shifted);
            }

            return new string(buffer);
        }

        public static string TitleToSnakeCase(this string input) => CapsRegex().Replace(input, " $1").Trim().ToLower().Replace(' ', '_');

        public static string SnakeToTitleCase(this string input) => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.Replace('_', ' '));

        public static string HashPassword(this string input, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(input, salt, 100000, HashAlgorithmName.SHA256);
            byte[] hashBytes = pbkdf2.GetBytes(256 / 8); // 256 bits (32 bytes)
            return Convert.ToBase64String(hashBytes);
        }

        public static string Obfuscate(this string input) => input.Caesar(59);

        public static string DeObfuscate(this string input) => input.Caesar(-59);
    }
}