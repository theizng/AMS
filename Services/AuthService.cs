using AMS.Data;
using AMS.Models;
using Microsoft.EntityFrameworkCore;
namespace AMS.Services
{
    public class AuthService : IAuthService
    {
        private readonly AMSDbContext _dbContext;
        private readonly ISecureStorage _secureStorage;
        private Admin _currentAdmin;

        public Admin CurrentAdmin => _currentAdmin;

        public AuthService(AMSDbContext dbContext, ISecureStorage secureStorage)
        {
            _dbContext = dbContext;
            _secureStorage = secureStorage;

            // Kiểm tra đăng nhập từ lưu trữ
            LoadAdminFromStorage();
        }
        public async Task ChangePasswordAsync(string currentPassword, string newPassword)
        {
            if (_currentAdmin == null)
            {
                throw new InvalidOperationException("Không có quản trị viên nào đang đăng nhập.");
            }
            bool passwordValid = BCrypt.Net.BCrypt.Verify(currentPassword, _currentAdmin.PasswordHash);
            if (!passwordValid)
            {
                throw new InvalidOperationException("Mật khẩu hiện tại không chính xác.");
            }
            // Mã hóa mật khẩu mới
            string newHashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
            // Cập nhật mật khẩu trong cơ sở dữ liệu
            _currentAdmin.PasswordHash = newHashedPassword;
            await _dbContext.SaveChangesAsync();
        }
        public async Task<AuthResult> LoginAsync(string username, string password)
        {
            try
            {
                var admin = await _dbContext.Admin
                    .FirstOrDefaultAsync(a => a.Username == username);

                if (admin == null)
                {
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage = "Tài khoản không tồn tại."
                    };
                }

                bool passwordValid = BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash);

                if (!passwordValid)
                {
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage = "Mật khẩu không chính xác."
                    };
                }

                // Cập nhật thời gian đăng nhập
                admin.LastLogin = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                // Lưu thông tin đăng nhập
                _currentAdmin = admin;
                await SaveAdminToStorageAsync(admin);

                return new AuthResult
                {
                    Success = true,
                    Admin = admin
                };
            }
            catch (Exception ex)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = $"Lỗi đăng nhập: {ex.Message}"
                };
            }
        }

        public bool IsLoggedIn()
        {
            return _currentAdmin != null;
        }

        public async Task LogoutAsync()
        {
            _currentAdmin = null;
            await _secureStorage.SetAsync("is_logged_in", "false");
            _secureStorage.Remove("admin_id");
        }

        public async Task<DateTime> UpdateLastLoginAsync()
        {
            if (_currentAdmin != null)
            {
                _currentAdmin.LastLogin = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                return _currentAdmin.LastLogin;
            }

            return DateTime.MinValue;
        }

        private async void LoadAdminFromStorage()
        {
            try
            {
                string isLoggedIn = await _secureStorage.GetAsync("is_logged_in");

                if (isLoggedIn == "true")
                {
                    string adminIdStr = await _secureStorage.GetAsync("admin_id");

                    if (!string.IsNullOrEmpty(adminIdStr) && int.TryParse(adminIdStr, out int adminId))
                    {
                        _currentAdmin = await _dbContext.Admin.FindAsync(adminId);
                    }
                }
            }
            catch
            {
                // Xử lý lỗi đọc storage
                _currentAdmin = null;
            }
        }

        private async Task SaveAdminToStorageAsync(Admin admin)
        {
            try
            {
                await _secureStorage.SetAsync("is_logged_in", "true");
                await _secureStorage.SetAsync("admin_id", admin.AdminId.ToString());
            }
            catch
            {
                // Xử lý lỗi lưu vào storage
            }
        }
    }
}