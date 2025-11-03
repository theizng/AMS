// Models/Invoice.cs
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AMS.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        public int RoomId { get; set; }

        // Quy ước: là ngày mồng 1 của tháng lập hóa đơn (UTC)
        [Required]
        public DateTime BillingMonth { get; set; } = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Các khoản cơ bản (đơn vị: VND)
        [Range(0, double.MaxValue)] public decimal BaseRent { get; set; } = 0m;
        [Range(0, double.MaxValue)] public decimal Utilities { get; set; } = 0m;   // điện/nước/internet
        [Range(0, double.MaxValue)] public decimal Extras { get; set; } = 0m;      // vệ sinh, bảo trì...
        [Range(0, double.MaxValue)] public decimal TotalAmount { get; set; } = 0m;

        [Range(0, double.MaxValue)] public decimal PaidAmount { get; set; } = 0m;

        [Required] public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;

        public string? Notes { get; set; }
        public string? UniqueCode { get; set; } // phục vụ QR/giao dịch

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Nav
        public Room? Room { get; set; }

        [NotMapped]
        public decimal Outstanding => Math.Max(0, TotalAmount - PaidAmount);
    }
}
