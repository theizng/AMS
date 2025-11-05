using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AMS.Services.Interfaces;

namespace AMS.Services.Interfaces
{
    // Returns RoomInfo used by the Google Sheet sync:
    // RoomCode, HouseAddress (Địa chỉ nhà), Active
    public interface IRoomsProvider
    {
        Task<IReadOnlyList<RoomInfo>> GetRoomsAsync(bool includeInactive = true, CancellationToken ct = default);
    }
}