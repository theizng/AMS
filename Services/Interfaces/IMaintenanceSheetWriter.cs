using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AMS.Services.Interfaces
{
    public record RoomInfo(string RoomCode, string? HouseAddress, string? TenantName, string? TenantPhone, bool Active = true);

    public interface IMaintenanceSheetWriter
    {
        Task UpdateAsync(string requestId, IDictionary<string, object> values, CancellationToken ct = default);
        Task<string> CreateAsync(IDictionary<string, object> values, CancellationToken ct = default);
        Task DeleteAsync(string requestId, CancellationToken ct = default);
        Task SyncRoomsAsync(IEnumerable<RoomInfo> rooms, CancellationToken ct = default);
    }
}