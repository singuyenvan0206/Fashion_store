using System;
using System.Security.Cryptography;
using System.Text;

namespace WpfApp1
{
    public static class PasswordHelper
    {
        /// <summary>
        /// Mã hóa mật khẩu sử dụng SHA256 với salt
        /// </summary>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            // Tạo salt ngẫu nhiên
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Kết hợp password và salt
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] saltedPassword = new byte[passwordBytes.Length + salt.Length];
            Buffer.BlockCopy(passwordBytes, 0, saltedPassword, 0, passwordBytes.Length);
            Buffer.BlockCopy(salt, 0, saltedPassword, passwordBytes.Length, salt.Length);

            // Hash password + salt
            byte[] hashBytes;
            using (var sha256 = SHA256.Create())
            {
                hashBytes = sha256.ComputeHash(saltedPassword);
            }

            // Kết hợp salt và hash để lưu trữ (salt:hash)
            byte[] result = new byte[salt.Length + hashBytes.Length];
            Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
            Buffer.BlockCopy(hashBytes, 0, result, salt.Length, hashBytes.Length);

            // Chuyển đổi sang Base64 để lưu trữ
            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Xác minh mật khẩu có khớp với hash đã lưu không
        /// </summary>
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
                return false;

            try
            {
                // Giải mã Base64 để lấy salt và hash
                byte[] storedBytes = Convert.FromBase64String(hashedPassword);
                
                // Lấy salt (16 bytes đầu tiên)
                byte[] salt = new byte[16];
                Buffer.BlockCopy(storedBytes, 0, salt, 0, 16);

                // Lấy hash đã lưu (32 bytes tiếp theo)
                byte[] storedHash = new byte[32];
                Buffer.BlockCopy(storedBytes, 16, storedHash, 0, 32);

                // Hash password nhập vào với salt đã lưu
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] saltedPassword = new byte[passwordBytes.Length + salt.Length];
                Buffer.BlockCopy(passwordBytes, 0, saltedPassword, 0, passwordBytes.Length);
                Buffer.BlockCopy(salt, 0, saltedPassword, passwordBytes.Length, salt.Length);

                byte[] computedHash;
                using (var sha256 = SHA256.Create())
                {
                    computedHash = sha256.ComputeHash(saltedPassword);
                }

                // So sánh hash
                return CompareByteArrays(storedHash, computedHash);
            }
            catch
            {
                // Nếu có lỗi (ví dụ: format không đúng), thử so sánh trực tiếp để tương thích với dữ liệu cũ
                return password == hashedPassword;
            }
        }

        /// <summary>
        /// So sánh hai mảng byte một cách an toàn
        /// </summary>
        private static bool CompareByteArrays(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length)
                return false;

            int result = 0;
            for (int i = 0; i < array1.Length; i++)
            {
                result |= array1[i] ^ array2[i];
            }

            return result == 0;
        }

        /// <summary>
        /// Kiểm tra xem một chuỗi có phải là hash đã được mã hóa không
        /// </summary>
        public static bool IsHashed(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            try
            {
                // Nếu có thể decode Base64 và có độ dài đúng (16 bytes salt + 32 bytes hash = 48 bytes)
                byte[] bytes = Convert.FromBase64String(password);
                return bytes.Length == 48;
            }
            catch
            {
                return false;
            }
        }
    }
}

