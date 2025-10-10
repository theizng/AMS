using System.Threading.Tasks;
using AMS.Models;

namespace AMS.Services
{
    public class LoginResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public Admin AdminData { get; set; }
        public string Token { get; set; }
    }

    public interface IAuthService
    {
        Task<LoginResult> LoginAsync(string username, string password);
        Task<bool> LogoutAsync();
        bool IsLoggedIn();
    }
}