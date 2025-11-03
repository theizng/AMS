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
            var ws = string.IsNullOrWhiteSpace(sheetName) ? wb.Worksheets.First() : wb.Worksheet(sheetName);
            var used = ws.RangeUsed();
            if (used == null)
                return Task.FromResult<IReadOnlyList<MaintenanceRequest>>(Array.Empty<MaintenanceRequest>());

            var header = used.FirstRow();
            int colCount = used.ColumnCount();

            // Map header text -> column index (1-based)
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int c = 1; c <= colCount; c++)
            {
                var name = header.Cell(c).GetString().Trim();
                if (!string.IsNullOrEmpty(name) && !map.ContainsKey(name))
                    map[name] = c;
            }

            int Resolve(string canonicalKey)
            {
                if (!MaintenanceSheetSchema.Aliases.TryGetValue(canonicalKey, out var aliases)) return 0;
                foreach (var alias in aliases)
                    if (map.TryGetValue(alias, out var idx)) return idx;
                return 0;
            }

            int cDate = Resolve(MaintenanceSheetSchema.Date);
            int cHouse = Resolve(MaintenanceSheetSchema.HouseAddress);
            int cRoom = Resolve(MaintenanceSheetSchema.RoomCode);
            int cName = Resolve(MaintenanceSheetSchema.TenantName);
            int cPhone = Resolve(MaintenanceSheetSchema.TenantPhone);
            int cCat = Resolve(MaintenanceSheetSchema.Category);
            int cDesc = Resolve(MaintenanceSheetSchema.Description);
            int cPri = Resolve(MaintenanceSheetSchema.Priority);
            int cStat = Resolve(MaintenanceSheetSchema.Status);
            int cAsn = Resolve(MaintenanceSheetSchema.AssignedTo);
            int cDue = Resolve(MaintenanceSheetSchema.DueDate);
            int cCost = Resolve(MaintenanceSheetSchema.EstimatedCost);

            var list = new List<MaintenanceRequest>();
            foreach (var row in used.RowsUsed().Skip(1))
            {
                string sHouse = cHouse > 0 ? row.Cell(cHouse).GetString().Trim() : "";
                string sRoom = cRoom > 0 ? row.Cell(cRoom).GetString().Trim() : "";
                string sDesc = cDesc > 0 ? row.Cell(cDesc).GetString().Trim() : "";
                if (string.IsNullOrEmpty(sHouse) && string.IsNullOrEmpty(sRoom) && string.IsNullOrEmpty(sDesc))
                    continue;

                // helper (optional)
                static IXLCell? SafeCell(IXLRangeRow row, int col) => col > 0 ? row.Cell(col) : null;

                var item = new MaintenanceRequest
                {
                    CreatedDate = TryGetDate(SafeCell(row, cDate), out var dt) ? dt : DateTime.Today,
                    HouseAddress = sHouse,
                    RoomCode = sRoom,
                    TenantName = cName > 0 ? row.Cell(cName).GetString().Trim() : "",
                    TenantPhone = cPhone > 0 ? row.Cell(cPhone).GetString().Trim() : "",
                    Category = cCat > 0 ? row.Cell(cCat).GetString().Trim() : "",
                    Description = sDesc,
                    Priority = cPri > 0 ? MaintenanceSheetSchema.NormalizePriority(row.Cell(cPri).GetString()) : "",
                    Status = cStat > 0 ? MaintenanceSheetSchema.ParseStatus(row.Cell(cStat).GetString()) : MaintenanceStatus.New,
                    AssignedTo = cAsn > 0 ? row.Cell(cAsn).GetString().Trim() : "",
                    DueDate = TryGetDate(SafeCell(row, cDue), out var due) ? due : null,
                    EstimatedCost = TryGetDecimal(SafeCell(row, cCost), out var est) ? est : null,
                    SourceRowInfo = $"{ws.Name}#{row.RowNumber()}"
                };

                list.Add(item);
            }

            return Task.FromResult<IReadOnlyList<MaintenanceRequest>>(list);
        }

        private static bool TryGetDate(IXLCell? cell, out DateTime date)
        {
            date = default;
            if (cell == null) return false;
            if (cell.TryGetValue<DateTime>(out var d)) { date = d.Date; return true; }
            var s = cell.GetString().Trim();
            if (DateTime.TryParse(s, out d)) { date = d.Date; return true; }
            return false;
        }

        private static bool TryGetDecimal(IXLCell? cell, out decimal value)
        {
            value = default;
            if (cell == null) return false;
            if (cell.TryGetValue<decimal>(out var v)) { value = v; return true; }
            var s = cell.GetString().Trim();
            if (string.IsNullOrEmpty(s)) return false;
            s = new string(s.Where(ch => char.IsDigit(ch) || ch == '.' || ch == ',').ToArray()).Replace(",", "");
            if (decimal.TryParse(s, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out v))
            {
                value = v; return true;
            }
            return false;
        }
    }
}