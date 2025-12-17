using System;
using System.Security.Cryptography;
using System.Text;

namespace FashionStore.Helpers
{
    public static class PasswordHasher
    {
        public static string HashPassword(string password)
        {
            // Tạo salt ngẫu nhiên
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Dùng HMACSHA256 để sinh hash với salt
            var hmac = new HMACSHA256(salt);
            var data = Encoding.UTF8.GetBytes(password);

            byte[] hash = hmac.ComputeHash(data);

            // Ghép salt + hash thành 1 mảng để lưu
            byte[] result = new byte[salt.Length + hash.Length];
            Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
            Buffer.BlockCopy(hash, 0, result, salt.Length, hash.Length);

            return Convert.ToBase64String(result);
        }

        // Kiểm tra mật khẩu khi login
        public static bool VerifyPassword(string password, string stored)
        {
            byte[] data = Convert.FromBase64String(stored);

            // Tách salt và hash đã lưu
            byte[] salt = new byte[16];
            Buffer.BlockCopy(data, 0, salt, 0, salt.Length);

            byte[] storedHash = new byte[data.Length - salt.Length];
            Buffer.BlockCopy(data, salt.Length, storedHash, 0, storedHash.Length);

            // Băm lại mật khẩu user nhập với salt đã tách
            var hmac = new HMACSHA256(salt);
            byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            // So sánh nội dung byte (constant-time)
            return SlowEquals(computedHash, storedHash);
        }

        private static bool SlowEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }

}