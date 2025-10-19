using System;
using System.Collections.Generic;
using System.Text;

namespace AMS.Models
{
    public class House
    {
        public int IdHouse { get; set; }
        public string? Address { get; set; }
        public int TotalRooms { get; set; }
        public string? Notes {   get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Room>? Rooms { get; set; } 
    }
}
