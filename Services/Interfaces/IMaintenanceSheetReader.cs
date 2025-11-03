using AMS.Models;

namespace AMS.Services.Interfaces
{
    public interface IMaintenanceSheetReader
    {
        Task<IReadOnlyList<MaintenanceRequest>> ReadAsync(string filePath, string? sheetName = null);
    }
}