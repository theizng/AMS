using System;
using System.Threading.Tasks;
using AMS.Models;

namespace AMS.Services.Interfaces
{
    public interface IAuthService
    {
        Admin CurrentAdmin { get; }

        Task<AuthResult> LoginAsync(string username, string password);
        Task LogoutAsync();
        bool IsLoggedIn();
        Task<DateTime> UpdateLastLoginAsync();
        Task ChangePasswordAsync(string currentPassword, string newPassword);

        // Update profile (already added earlier)
        Task UpdateProfileAsync(string fullName, string email, string phone, string? idCardNumber);

        // NEW: Forgot password flow
        Task<ForgotPasswordResult> ForgotPasswordAsync(string email);

    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Admin? Admin { get; set; }
    }

    public class ForgotPasswordResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? TempPassword { get; set; } // For logging/debug (DO NOT display to end-user)
    }
}