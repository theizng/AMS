using System;

namespace AMS.Models
{
    public class FeeType
    {
        public string FeeTypeId { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; }
        public bool IsRecurring { get; set; }
        public string? UnitLabel { get; set; }  // e.g., "m3", "lần", null for flat
        public decimal DefaultRate { get; set; }
        public bool ApplyAllRooms { get; set; }
        public bool Active { get; set; } = true;
    }
}