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

        public decimal UtilityFeesTotal => WaterAmount + ElectricAmount;
        public decimal CustomFeesTotal { get; set; }
        public decimal ElectricAmount { get; set; }
        public decimal WaterAmount { get; set; }

        // Persisted BikePrice (migration already applied)
        public decimal BikePrice { get; set; }

        public decimal TotalDue => BaseRent + UtilityFeesTotal + CustomFeesTotal + BikePrice;
        public decimal AmountPaid { get; set; }
        public decimal AmountRemaining => TotalDue - AmountPaid;

        public DateTime? FirstSentAt { get; set; }
        public DateTime? LastReminderSentAt { get; set; }
        public DateTime? PaidAt { get; set; }

        public List<FeeInstance> Fees { get; set; } = new();
        public ElectricReading? ElectricReading { get; set; }
        public WaterReading? WaterReading { get; set; }
        public List<PaymentRecord> Payments { get; set; } = new();

        /// <summary>
        /// Computes bike parking fee using: max(0, activeBikeCount - FreeBikeAllowance) * BikeExtraFee.
        /// </summary>
        public static decimal CalculateBikePrice(Room room, int activeBikeCount)
        {
            if (room == null) return 0m;
            if (room.BikeExtraFee is null || room.BikeExtraFee <= 0m) return 0m;

            var chargeable = activeBikeCount - room.FreeBikeAllowance;
            if (chargeable < 0) chargeable = 0;
            return chargeable * room.BikeExtraFee.Value;
        }
    }
}