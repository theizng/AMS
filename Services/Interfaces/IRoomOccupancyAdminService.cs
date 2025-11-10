using System.Threading.Tasks;

namespace AMS.Services.Interfaces
{
    public interface IRoomOccupancyAdminService
    {
        /// <summary>
        /// Ends all active occupancies (MoveOutDate = today) for the room, clears BikeCount,
        /// deletes all bikes belonging to those tenants in that room, and clears emergency contact preference.
        /// No-ops if room code is invalid.
        /// </summary>
        Task EndAllActiveOccupanciesForRoomAsync(string roomCode);
    }
}