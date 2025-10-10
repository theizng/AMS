using System;
using System.Threading.Tasks;
using AMS.Models;
using System.Security.Cryptography;
using System.Text;

namespace AMS.Services
{
    public class AuthService : IAuthService
    {
        // Demo admin (trong thực tế sẽ lấy từ database)
        private readonly Admin _demoAdmin = new Admin
        {
            Id = "1",
            Username = "admin",
            PasswordHash = "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8", // "password"
            FullName = "Nguyen Van A",
            Email = "admin@example.com",
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // Lưu trạng thái đăng nhập
        private Admin _currentAdmin;
        private string _authToken;

        public async Task<LoginResult> LoginAsync(string username, string password)
        {
            // Giả lập độ trễ mạng
            await Task.Delay(500);

            // Hash mật khẩu để so sánh
            string hashedPassword = ComputeSha256Hash(password);

            // Kiểm tra thông tin đăng nhập
            if (username == _demoAdmin.Username &&
                hashedPassword == _demoAdmin.PasswordHash)
            {
                // Cập nhật thông tin đăng nhập
                _demoAdmin.LastLoginDate = DateTime.Now;

                // Lưu thông tin đăng nhập
                _currentAdmin = _demoAdmin;
                _authToken = Guid.NewGuid().ToString();

                // Lưu token vào Preferences
                Preferences.Set("auth_token", _authToken);

                return new LoginResult
                {
                    Success = true,
                    AdminData = _currentAdmin,
                    Token = _authToken
                };
            }

            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Ten dang nhap hoac mat khau khong chinh xac"
            };
        }

        public Task<bool> LogoutAsync()
        {
            _currentAdmin = null;
            _authToken = null;
            Preferences.Remove("auth_token");
            return Task.FromResult(true);
        }

        public bool IsLoggedIn()
        {
            // Kiểm tra xem đã đăng nhập chưa
            return _currentAdmin != null || Preferences.ContainsKey("auth_token");
        }

        private string ComputeSha256Hash(string rawData)
        {
            // Đối với demo, trả về giá trị cố định cho "password"
            if (rawData == "password")
                return "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8";

            // Hash thực tế cho các mật khẩu khác
            using (var sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}