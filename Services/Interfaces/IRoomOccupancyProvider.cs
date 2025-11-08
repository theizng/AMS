using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AMS.Services.Interfaces
{
    public interface IRoomOccupancyProvider
    {
        /// <summary>
        /// Returns current active tenants for the given room code.
        /// </summary>
        Task<IReadOnlyList<RoomTenantDto>> GetTenantsForRoomAsync(string roomCode, CancellationToken ct = default);
    }

    public class RoomTenantDto
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
    }
}