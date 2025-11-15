using System;

namespace AMS.Models
{
    public class Admin
    {
        // Thuộc tính nhận dạng quản trị viên
        public int AdminId { get; set; } //Primary key column

        // Thông tin đăng nhập
        public string Username { get; set; }
        public string PasswordHash { get; set; }

        // Thông tin liên hệ (triển khai IContactInfo)
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string? IdCardNumber { get; set; }
        // Thông tin đăng nhập và hệ thống (chỉ giữ cái cần thiết)
        public DateTime LastLogin { get; set; }

        // Thông tin hệ thống - giữ lại để theo dõi
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}