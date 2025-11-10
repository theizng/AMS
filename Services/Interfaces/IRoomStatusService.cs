using System.Threading.Tasks;

namespace AMS.Services.Interfaces
{
    public interface IRoomStatusService
    {
        Task SetRoomOccupiedAsync(string roomCode);
        Task SetRoomAvailableAsync(string roomCode);
    }
}