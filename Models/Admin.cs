using System;

namespace AMS.Models
{
    public class Admin
    {
        // Thuộc tính nhận dạng quản trị viên
        public string Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }

        // Thông tin cá nhân của chủ nhà/quản lý
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        // Thông tin xác thực và hệ thống
        public DateTime LastLoginDate { get; set; }
        public string RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryDate { get; set; }
        public bool IsActive { get; set; }

        // Thông tin hệ thống
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}