using System;

namespace AMS.Models
{
    public class FeeInstance
    {
        public string FeeInstanceId { get; set; } = Guid.NewGuid().ToString("N");
        public string RoomChargeId { get; set; }
        public string FeeTypeId { get; set; }
        public string Name { get; set; }
        public decimal Rate { get; set; }
        public decimal Quantity { get; set; } = 1;
        public decimal Amount => Rate * Quantity;
    }
}