using System.Globalization;

namespace AMS.Models
{
    public enum MaintenanceStatus { New, InProgress, Done, Cancelled }

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
        // Canonical value or raw from sheet ("Low/Medium/High" or "Thấp/Trung bình/Cao")
        public string Priority { get; set; } = "";
        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.New;
        public string AssignedTo { get; set; } = "";
        public DateTime? DueDate { get; set; }
        public decimal? EstimatedCost { get; set; }
        public string SourceRowInfo { get; set; } = "";

        public string StatusVi => Status switch
        {
            MaintenanceStatus.New => "Chưa xử lý",
            MaintenanceStatus.InProgress => "Đang xử lý",
            MaintenanceStatus.Done => "Đã xử lý",
            MaintenanceStatus.Cancelled => "Đã hủy",
            _ => "Chưa xử lý"
        };

        // Always show Vietnamese, accept VN or EN source values.
        public string PriorityVi
        {
            get
            {
                var p = (Priority ?? "").Trim().ToLowerInvariant();
                return p switch
                {
                    "thấp" or "thap" or "low" => "Thấp",
                    "trung bình" or "trung binh" or "medium" => "Trung bình",
                    "cao" or "high" => "Cao",
                    _ => ""
                };
            }
        }
    }
}