using System;
using System.Collections.Generic;

namespace AMS.Models
{
    public class PaymentCycle
    {
        public string CycleId { get; set; } = Guid.NewGuid().ToString("N");
        public int Year { get; set; }
        public int Month { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool Closed { get; set; }
        public DateTime? ClosedAt { get; set; }

        public List<RoomCharge> RoomCharges { get; set; } = new();
    }
}