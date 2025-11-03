using AMS.Models;

namespace AMS.Services
{
    public static class MaintenanceSheetSchema
    {
        public const string DefaultSheetName = "Maintenance";

        public const string RequestId = "RequestId"; // NEW
        public const string Date = "Date";
        public const string HouseAddress = "HouseAddress";
        public const string RoomCode = "RoomCode";
        public const string TenantName = "TenantName";
        public const string TenantPhone = "TenantPhone";
        public const string Category = "Category";
        public const string Description = "Description";
        public const string Priority = "Priority";
        public const string Status = "Status";
        public const string AssignedTo = "AssignedTo";
        public const string DueDate = "DueDate";
        public const string EstimatedCost = "EstimatedCost";

        public static readonly Dictionary<string, string[]> Aliases = new(StringComparer.OrdinalIgnoreCase)
        {
            [RequestId] = new[] { "RequestId", "ID", "Mã yêu cầu", "Ma yeu cau" },
            [Date] = new[] { "Date", "Created", "CreatedDate", "Thời gian tạo", "Ngày tạo" },
            [HouseAddress] = new[] { "HouseAddress", "House", "Địa chỉ nhà", "Địa chỉ", "Nhà" },
            [RoomCode] = new[] { "RoomCode", "Room", "Mã phòng", "Phòng" },
            [TenantName] = new[] { "TenantName", "Tenant", "Tên khách thuê", "Người thuê", "Khách thuê" },
            [TenantPhone] = new[] { "TenantPhone", "Phone", "Số điện thoại", "Điện thoại", "SĐT" },
            [Category] = new[] { "Category", "Phân loại", "Nhóm" },
            [Description] = new[] { "Description", "Issue", "Miêu tả", "Mô tả", "Nội dung" },
            [Priority] = new[] { "Priority", "Mức độ ưu tiên", "Ưu tiên" },
            [Status] = new[] { "Status", "Trạng thái" },
            [AssignedTo] = new[] { "AssignedTo", "Assignee", "Người phụ trách" },
            [DueDate] = new[] { "DueDate", "Due", "Hạn", "Hạn xử lý" },
            [EstimatedCost] = new[] { "EstimatedCost", "Cost", "Chi phí (nếu có)", "Chi phí", "Ước tính" },
        };

        public static MaintenanceStatus ParseStatus(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return MaintenanceStatus.New;
            switch (text.Trim().ToLower ()) { } // placeholder to avoid accidental errors in summary
            // See your earlier implementation; keep as-is
            return MaintenanceStatus.New;
        }

        public static string NormalizePriority(string? text)
        {
            // Keep the implementation you already added
            return string.IsNullOrWhiteSpace(text) ? "" : text.Trim();
        }
    }
}