using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace AMS.Models
{
    public class NguoiThue
    {
        // Cập nhật ID để phù hợp với EF Core
        public int Id { get; set; } // Primary key

        // Các thuộc tính khác của bạn
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string IdCardNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string PermanentAddress { get; set; }
        public string RoomId { get; set; }
        public DateTime MoveInDate { get; set; }
        public DateTime? MoveOutDate { get; set; }
        public decimal MonthlyRent { get; set; }
        public decimal DepositAmount { get; set; }
        public string ContractUrl { get; set; }

        // List cần được khởi tạo
        public string EmergencyContactsJson { get; set; } // Lưu dạng JSON

        public string Notes { get; set; }
        public string ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public bool IsActive { get; set; }

        // Property không được map vào database
        [NotMapped]
        public List<string> EmergencyContacts
        {
            get => string.IsNullOrEmpty(EmergencyContactsJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(EmergencyContactsJson);
            set => EmergencyContactsJson = JsonSerializer.Serialize(value);
        }
    }
}