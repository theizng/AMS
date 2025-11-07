using AMS.Models;
using AMS.Services.Interfaces;
using ClosedXML.Excel;

namespace AMS.Services
{
    public class ClosedXMLMaintenanceSheetReader : IMaintenanceSheetReader
    {
        public Task<IReadOnlyList<MaintenanceRequest>> ReadAsync(string filePath, string? sheetName = null)
        {
            using var wb = new XLWorkbook(filePath);

            var ws = string.IsNullOrWhiteSpace(sheetName) ? wb.Worksheet(1) : wb.Worksheet(sheetName);

            var used = ws.RangeUsed();
            if (used == null) return Task.FromResult<IReadOnlyList<MaintenanceRequest>>(Array.Empty<MaintenanceRequest>());

            // Build header map: header text (trim, case-insensitive) -> column index (1-based)
            var header = used.FirstRow();
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int colCount = used.ColumnCount();

            for (int c = 1; c <= colCount; c++)
            {
                var name = header.Cell(c).GetString().Trim();
                if (!string.IsNullOrEmpty(name)) map[name] = c;
            }

            int Get(params string[] aliases)
            {
                foreach (var a in aliases)
                    if (map.TryGetValue(a, out var idx)) return idx;
                return -1;
            }

            // Include RequestId and VN aliases
            int cReqId = Get("RequestId", "ID", "Mã yêu cầu", "Ma yeu cau");
            int cDate = Get("Date", "Created", "CreatedDate", "Thời gian tạo", "Ngày tạo");
            int cHouse = Get("House", "HouseAddress", "Địa chỉ nhà", "Địa chỉ", "Nhà");
            int cRoom = Get("Room", "RoomCode", "Mã phòng", "Phòng");
            int cTenant = Get("TenantName", "Tenant", "Tên khách thuê", "Người thuê", "Khách thuê");
            int cPhone = Get("Phone", "TenantPhone", "Số điện thoại", "Điện thoại", "SĐT");
            int cCategory = Get("Category", "Phân loại", "Nhóm");
            int cDesc = Get("Description", "Issue", "Miêu tả", "Mô tả", "Nội dung");
            int cPriority = Get("Priority", "Mức độ ưu tiên", "Ưu tiên");
            int cStatus = Get("Status", "Trạng thái");
            int cAssigned = Get("AssignedTo", "Assignee", "Người phụ trách");
            int cDue = Get("Due", "DueDate", "Hạn", "Hạn xử lý");
            int cCost = Get("Cost", "EstimatedCost", "Chi phí (nếu có)", "Chi phí", "Ước tính");

            var list = new List<MaintenanceRequest>();

            foreach (var row in used.RowsUsed().Skip(1))
            {
                var houseVal = ReadCellAsString(row, cHouse);
                var descVal = ReadCellAsString(row, cDesc);

                // Skip blank rows (no house + no description)
                if (string.IsNullOrEmpty(houseVal) && string.IsNullOrEmpty(descVal))
                    continue;

                // Created date (safe)
                DateTime created = DateTime.Today;
                if (cDate > 0)
                {
                    var cell = row.Cell(cDate);
                    if (cell.TryGetValue<DateTime>(out var dt)) created = dt.Date;
                    else if (DateTime.TryParse(cell.GetString().Trim(), out var dt2)) created = dt2.Date;
                }

                // Due date (safe)
                DateTime? due = null;
                if (cDue > 0)
                {
                    var cell = row.Cell(cDue);
                    if (cell.TryGetValue<DateTime>(out var d)) due = d.Date;
                    else if (DateTime.TryParse(cell.GetString().Trim(), out var d2)) due = d2.Date;
                }

                // Cost (safe)
                decimal? cost = null;
                if (cCost > 0)
                {
                    var cell = row.Cell(cCost);
                    if (cell.TryGetValue<decimal>(out var v)) cost = v;
                    else
                    {
                        var s = cell.GetString().Trim();
                        if (!string.IsNullOrEmpty(s))
                        {
                            s = new string(s.Where(ch => char.IsDigit(ch) || ch == '.' || ch == ',').ToArray()).Replace(",", "");
                            if (decimal.TryParse(s, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var v2))
                                cost = v2;
                        }
                    }
                }

                // Status mapping (includes Vietnamese variants)
                var statusText = ReadCellAsString(row, cStatus);
                var t = statusText?.Trim().ToLowerInvariant();
                var status = t switch
                {
                    "new" or "mới" or "moi" or "chưa xử lý" or "chua xu ly" => MaintenanceStatus.New,
                    "inprogress" or "in progress" or "đang xử lý" or "dang xu ly" => MaintenanceStatus.InProgress,
                    "done" or "hoàn tất" or "hoan tat" or "đã xử lý" or "da xu ly" => MaintenanceStatus.Done,
                    "cancelled" or "canceled" or "đã hủy" or "da huy" => MaintenanceStatus.Cancelled,
                    _ => MaintenanceStatus.New
                };

                list.Add(new MaintenanceRequest
                {
                    RequestId = ReadCellAsString(row, cReqId),   // <-- now filled
                    CreatedDate = created,
                    HouseAddress = houseVal ?? "",
                    RoomCode = ReadCellAsString(row, cRoom),
                    TenantName = ReadCellAsString(row, cTenant),
                    TenantPhone = ReadCellAsString(row, cPhone),
                    Category = ReadCellAsString(row, cCategory),
                    Description = descVal ?? "",
                    Priority = ReadCellAsString(row, cPriority),
                    Status = status,
                    AssignedTo = ReadCellAsString(row, cAssigned),
                    DueDate = due,
                    EstimatedCost = cost,
                    SourceRowInfo = $"{ws.Name}#{row.RowNumber()}"
                });
            }

            return Task.FromResult<IReadOnlyList<MaintenanceRequest>>(list);
        }

        private static string ReadCellAsString(IXLRangeRow row, int colIndex)
        {
            if (colIndex <= 0) return "";
            var cell = row.Cell(colIndex);
            // Prefer GetString(); if empty (numeric/formatted), fall back to Value.ToString()
            var s = cell.GetString();
            if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
            var v = cell.Value;
            return v.ToString()?.Trim() ?? "";
        }
    }
}