using AMS.Models;

namespace AMS.Services.Interfaces
{
    public interface IOnlineMaintenanceReader
    {
        // Accepts any standard Google Sheets URL. Implementation should derive a direct export URL and read.
        Task<IReadOnlyList<MaintenanceRequest>> ReadFromUrlAsync(string sheetUrl, string? sheetName = null, CancellationToken ct = default);
    }
}