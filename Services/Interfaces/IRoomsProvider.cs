using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AMS.Services; // <--- ensure RoomInfo type is referenced from AMS.Services

namespace AMS.Services.Interfaces
{
    // Returns RoomInfo used by the Google Sheet sync and Contract creation:
    // RoomCode, HouseAddress (Địa chỉ nhà), Active, Price, MaxOccupants, etc.
    public interface IRoomsProvider
    {
        Task<IReadOnlyList<RoomInfo>> GetRoomsAsync(bool includeInactive = true, CancellationToken ct = default);
    }
}