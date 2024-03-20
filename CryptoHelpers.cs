using System.Security.Cryptography;

namespace SnapApp.Svc
{
    public static class CryptoHelpers
    {
        public static byte[] GenerateSalt()
        {
            byte[] salt = new byte[128 / 8]; // 128 bits (16 bytes)
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);

            return salt;
        }
    }
}