using System;

namespace AMS.Models
{
    // Optional entity: create when you want to persist bikes
    public class Bike
    {
        public int Id { get; set; }
        public int RoomId { get; set; }

        public string Plate { get; set; } = "";

        //public string? OwnerName { get; set; }
        public int OwnerId { get; set; }
        public Tenant? OwnerTenant { get; set; }    
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Nav
        public Room? Room { get; set; }
    }
}