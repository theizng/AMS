using AMS.Models;
namespace AMS.Services
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string username, string password);
        bool IsLoggedIn();
        Task LogoutAsync();
        Task<DateTime> UpdateLastLoginAsync();
        Admin CurrentAdmin { get; }
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public Admin Admin { get; set; }
    }
}