using System;
using System.Collections.Generic;
using System.Text;

namespace AMS.Models
{
    public class Nha
    {
        public int Id { get; set; }
        public string? DiaChi { get; set; }
        public int TotalRooms { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
