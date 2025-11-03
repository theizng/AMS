using System;
using System.Collections.Generic;
using System.Text;

namespace AMS.Models
{
    public enum MaintenanceStatus
    {
        New,
        InProgress,
        Done,
        Cancelled
    }

    public class MaintenanceRequest
    {
        public string? RequestId { get; set; } 
        public DateTime CreatedDate { get; set; }
        public string HouseAddress { get; set; } = "";
        public string RoomCode { get; set; } = "";
        public string TenantName { get; set; } = "";
        public string TenantPhone { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public string Priority { get; set; } = ""; // e.g., Low/Medium/High
        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.New;
        public string AssignedTo { get; set; } = "";
        public DateTime? DueDate { get; set; }
        public decimal? EstimatedCost { get; set; }
        public string SourceRowInfo { get; set; } = ""; // e.g., sheet name + row number
    }
}