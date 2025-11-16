using System.Collections.Generic;
using System.Threading.Tasks;
using AMS.Models;

namespace AMS.Services.Interfaces
{
    public interface IMeterSheetReader
    {
        Task<IReadOnlyList<MeterRow>> ReadAsync(string filePath);
    }

    public interface IOnlineMeterSheetReader
    {
        Task<IReadOnlyList<MeterRow>> ReadFromUrlAsync(string sheetUrl);
    }

    public interface IMeterSheetWriter
    {
        Task UpdateRowAsync(string sheetUrlOrPath, MeterRow row); // optional
        Task ApplyValidationsAsync(string sheetUrlOrPath);        // optional
    }

    public class MeterRow
    {
        public string RoomCode { get; set; } = "";
        public int? PreviousElectric { get; set; }
        public int? CurrentElectric { get; set; }
        public int? ConsumptionElectric { get; set; }

        public int? PreviousWater { get; set; }
        public int? CurrentWater { get; set; }
        public int? ConsumptionWater { get; set; }
    }
}