using System;

namespace AMS.Models
{
    public class PaymentRecord
    {
        public string PaymentRecordId { get; set; } = Guid.NewGuid().ToString("N");
        public string RoomChargeId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; } = DateTime.UtcNow;
        public string? Note { get; set; }
        public bool IsPartial { get; set; }
    }
}