using System;
using System.Collections.Generic;

namespace AMS.Models
{
    public class RoomCharge
    {
        public string RoomChargeId { get; set; } = Guid.NewGuid().ToString("N");
        public string CycleId { get; set; }
        public string RoomCode { get; set; }
        public decimal BaseRent { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.MissingData;

        public decimal TotalFees => (UtilityFeesTotal + CustomFeesTotal);
        public decimal UtilityFeesTotal { get; set; }
        public decimal CustomFeesTotal { get; set; }
        public decimal ElectricAmount { get; set; }
        public decimal WaterAmount { get; set; }

        public decimal TotalDue => BaseRent + ElectricAmount + WaterAmount + CustomFeesTotal;
        public decimal AmountPaid { get; set; }
        public decimal AmountRemaining => TotalDue - AmountPaid;

        public DateTime? FirstSentAt { get; set; }
        public DateTime? LastReminderSentAt { get; set; }
        public DateTime? PaidAt { get; set; }

        public List<FeeInstance> Fees { get; set; } = new();
        public ElectricReading? ElectricReading { get; set; }
        public WaterReading? WaterReading { get; set; }
        public List<PaymentRecord> Payments { get; set; } = new();
    }
}