using System.Threading.Tasks;

namespace AMS.Services.Interfaces
{
    public interface IContractRoomGuard
    {
        Task<bool> CanEditRoomAsync(string roomCode);
    }
}