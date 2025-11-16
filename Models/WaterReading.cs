using System;

namespace AMS.Models
{
    public class WaterReading
    {
        public string WaterReadingId { get; set; } = Guid.NewGuid().ToString("N");
        public string RoomChargeId { get; set; }
        public int Previous { get; set; }
        public int Current { get; set; }
        public int Consumption => Current - Previous;
        public decimal Rate { get; set; }  // VND / m3
        public decimal Amount => Consumption * Rate;
        public bool Confirmed { get; set; }
    }
}