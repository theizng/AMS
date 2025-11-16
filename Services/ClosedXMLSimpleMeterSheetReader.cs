using AMS.Services.Interfaces;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AMS.Services
{
    public class ClosedXMLSimpleMeterSheetReader : IMeterSheetReader
    {
        public Task<IReadOnlyList<MeterRow>> ReadAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return Task.FromResult<IReadOnlyList<MeterRow>>(Array.Empty<MeterRow>());

            using var wb = new XLWorkbook(filePath);
            // Pick sheet by name or first
            var ws = wb.Worksheets.FirstOrDefault(s =>
                s.Name.Equals("Chỉ số điện / nước", StringComparison.OrdinalIgnoreCase)) ?? wb.Worksheet(1);

            var used = ws.RangeUsed();
            if (used == null || used.RowCount() < 2) return Task.FromResult<IReadOnlyList<MeterRow>>(Array.Empty<MeterRow>());

            // Build header array
            var headerRow = used.FirstRow();
            var headerCells = headerRow.Cells().Select(c => c.GetString()).ToArray();

            // Map indexes by position (1-based)
            int colRoom = 1;
            int colPrevElec = 2;
            int colCurElec = 3;
            int colConsElec = 4;
            int colPrevWater = 5;
            int colCurWater = 6;
            int colConsWater = 7;

            // If user kept duplicate names, rely on position; else attempt detection
            if (headerCells.Length >= 7)
            {
                // Optional detection override
                // Electric previous/current
                var prevElecIdx = headerCells
                    .Select((h, i) => new { h, i })
                    .FirstOrDefault(x => MeterSimpleSheetSchema.LooksElectricPrevious(x.h))?.i;
                var curElecIdx = headerCells
                    .Select((h, i) => new { h, i })
                    .FirstOrDefault(x => MeterSimpleSheetSchema.LooksElectricCurrent(x.h))?.i;

                var prevWaterIdx = headerCells
                    .Select((h, i) => new { h, i })
                    .FirstOrDefault(x => MeterSimpleSheetSchema.LooksWaterPrevious(x.h))?.i;
                var curWaterIdx = headerCells
                    .Select((h, i) => new { h, i })
                    .FirstOrDefault(x => MeterSimpleSheetSchema.LooksWaterCurrent(x.h))?.i;

                if (prevElecIdx.HasValue) colPrevElec = prevElecIdx.Value + 1;
                if (curElecIdx.HasValue) colCurElec = curElecIdx.Value + 1;
                if (prevWaterIdx.HasValue) colPrevWater = prevWaterIdx.Value + 1;
                if (curWaterIdx.HasValue) colCurWater = curWaterIdx.Value + 1;
            }

            var list = new List<MeterRow>();

            foreach (var row in used.RowsUsed().Skip(1))
            {
                var roomCode = ReadString(row, colRoom);
                if (string.IsNullOrWhiteSpace(roomCode)) continue;

                int? prevElec = TryInt(row, colPrevElec);
                int? curElec = TryInt(row, colCurElec);
                int? consElec = TryInt(row, colConsElec); // may be formula blank

                int? prevWater = TryInt(row, colPrevWater);
                int? curWater = TryInt(row, colCurWater);
                int? consWater = TryInt(row, colConsWater);

                // If consumption not provided (no formula or blank), calculate
                if (!consElec.HasValue && prevElec.HasValue && curElec.HasValue)
                    consElec = curElec - prevElec;
                if (!consWater.HasValue && prevWater.HasValue && curWater.HasValue)
                    consWater = curWater - prevWater;

                list.Add(new MeterRow
                {
                    RoomCode = roomCode.Trim(),
                    PreviousElectric = prevElec,
                    CurrentElectric = curElec,
                    ConsumptionElectric = consElec,
                    PreviousWater = prevWater,
                    CurrentWater = curWater,
                    ConsumptionWater = consWater
                });
            }

            return Task.FromResult<IReadOnlyList<MeterRow>>(list);
        }

        private static string ReadString(IXLRangeRow row, int col)
        {
            if (col <= 0) return "";
            var cell = row.Cell(col);
            var s = cell.GetString();
            return !string.IsNullOrWhiteSpace(s) ? s.Trim() : cell.Value.ToString()?.Trim() ?? "";
        }

        private static int? TryInt(IXLRangeRow row, int col)
        {
            if (col <= 0) return null;
            var cell = row.Cell(col);
            if (cell.DataType == XLDataType.Number)
                return (int)Math.Round(cell.GetDouble());
            var s = cell.GetString().Trim();
            if (int.TryParse(s, out var v)) return v;
            return null;
        }
    }
}