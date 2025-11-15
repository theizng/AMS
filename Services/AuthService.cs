using AMS.Data;
using AMS.Models;
using AMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

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
            LoadAdminFromStorage();
        }

        public async Task ChangePasswordAsync(string currentPassword, string newPassword)
        {
            if (_currentAdmin == null)
                throw new InvalidOperationException("Không có quản trị viên nào đang đăng nhập.");

            bool passwordValid = BCrypt.Net.BCrypt.Verify(currentPassword, _currentAdmin.PasswordHash);
            if (!passwordValid)
                throw new InvalidOperationException("Mật khẩu hiện tại không chính xác.");

            string newHashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _currentAdmin.PasswordHash = newHashedPassword;
            _currentAdmin.UpdatedAt = DateTime.UtcNow;

            // Clear temp flag if existed
            Preferences.Remove("admin:pwd:temp");
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateProfileAsync(string fullName, string email, string phone, string? idCardNumber)
        {
            if (_currentAdmin == null)
                throw new InvalidOperationException("Không có quản trị viên nào đang đăng nhập.");

            _currentAdmin.FullName = fullName?.Trim() ?? "";
            _currentAdmin.Email = email?.Trim() ?? "";
            _currentAdmin.PhoneNumber = phone?.Trim() ?? "";
            _currentAdmin.IdCardNumber = string.IsNullOrWhiteSpace(idCardNumber) ? null : idCardNumber.Trim();
            _currentAdmin.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
        }

        public async Task<AuthResult> LoginAsync(string username, string password)
        {
            try
            {
                var admin = await _dbContext.Admin
                    .FirstOrDefaultAsync(a => a.Username == username);

                if (admin == null)
                    return new AuthResult { Success = false, ErrorMessage = "Tài khoản không tồn tại." };

                bool passwordValid = BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash);
                if (!passwordValid)
                    return new AuthResult { Success = false, ErrorMessage = "Mật khẩu không chính xác." };

                admin.LastLogin = DateTime.UtcNow;
                admin.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                _currentAdmin = admin;
                await SaveAdminToStorageAsync(admin);

                return new AuthResult { Success = true, Admin = admin };
            }
            catch (Exception ex)
            {
                return new AuthResult { Success = false, ErrorMessage = $"Lỗi đăng nhập: {ex.Message}" };
            }
        }

        public bool IsLoggedIn() => _currentAdmin != null;

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
                _currentAdmin.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                return _currentAdmin.LastLogin;
            }
            return DateTime.MinValue;
        }

        // FORGOT PASSWORD
        public async Task<ForgotPasswordResult> ForgotPasswordAsync(string email)
        {
            try
            {
                var admin = await _dbContext.Admin.FirstOrDefaultAsync(a => a.Email == email);
                if (admin == null)
                {
                    return new ForgotPasswordResult
                    {
                        Success = false,
                        ErrorMessage = "Email không tồn tại trong hệ thống."
                    };
                }

                var tempPassword = GenerateTemporaryPassword();
                var hashed = BCrypt.Net.BCrypt.HashPassword(tempPassword);

                admin.PasswordHash = hashed;
                admin.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                // Flag that password is temporary (for Settings reminder)
                Preferences.Set("admin:pwd:temp", true);

                // We do NOT expose temp password through UI; only for email sending
                return new ForgotPasswordResult
                {
                    Success = true,
                    TempPassword = tempPassword
                };
            }
            catch (Exception ex)
            {
                return new ForgotPasswordResult
                {
                    Success = false,
                    ErrorMessage = $"Lỗi: {ex.Message}"
                };
            }
        }

        private static string GenerateTemporaryPassword(int length = 12)
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string specials = "!@#$%*_+-";
            string all = upper + lower + digits + specials;

            var bytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);

            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(all[bytes[i] % all.Length]);
            }
            return sb.ToString();
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
                // ignore
            }
        }
    }
}