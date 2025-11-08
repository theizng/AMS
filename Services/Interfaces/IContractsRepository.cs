using AMS.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AMS.Services.Interfaces
{
    public interface IContractsRepository
    {
        Task<IReadOnlyList<Contract>> GetAllAsync(CancellationToken ct = default);
        Task<Contract?> GetByIdAsync(string contractId, CancellationToken ct = default);
        Task CreateAsync(Contract contract, CancellationToken ct = default);
        Task UpdateAsync(Contract contract, CancellationToken ct = default);
        Task DeleteAsync(string contractId, CancellationToken ct = default);

        Task<bool> RoomHasActiveContractAsync(string roomCode, DateTime asOf, CancellationToken ct = default);
        Task<Contract?> GetActiveContractByRoomAsync(string roomCode, DateTime asOf, CancellationToken ct = default);
        Task MarkNeedsAddendumAsync(string roomCode, CancellationToken ct = default);
    }
}