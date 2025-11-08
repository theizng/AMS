using AMS.Data;
using AMS.Models;
using AMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AMS.Services
{
    public class ContractsRepository : IContractsRepository
    {
        private readonly AMSDbContext _db;

        public ContractsRepository(AMSDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<Contract>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.Contracts
                .AsNoTracking()
                .OrderByDescending(c => c.Status == ContractStatus.Active)
                .ThenBy(c => c.EndDate) // use mapped column; equivalent ordering
                .ToListAsync(ct);
        }

        public async Task<Contract?> GetByIdAsync(string contractId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(contractId)) return null;
            return await _db.Contracts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ContractId == contractId, ct);
        }

        public async Task CreateAsync(Contract contract, CancellationToken ct = default)
        {
            if (contract == null) throw new ArgumentNullException(nameof(contract));

            // Ensure no overlapping active contract for the room
            if (await RoomHasActiveContractAsync(contract.RoomCode, DateTime.Today, ct))
                throw new InvalidOperationException("Room already has an active contract.");

            contract.CreatedAt = DateTime.UtcNow;
            contract.UpdatedAt = DateTime.UtcNow;

            _db.Contracts.Add(contract);
            await _db.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Contract contract, CancellationToken ct = default)
        {
            if (contract == null) throw new ArgumentNullException(nameof(contract));

            contract.UpdatedAt = DateTime.UtcNow;
            _db.Contracts.Update(contract);
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(string contractId, CancellationToken ct = default)
        {
            var e = await _db.Contracts.FirstOrDefaultAsync(x => x.ContractId == contractId, ct);
            if (e == null) return;
            _db.Contracts.Remove(e);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<bool> RoomHasActiveContractAsync(string roomCode, DateTime asOf, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return false;
            return await _db.Contracts.AnyAsync(c =>
                c.RoomCode == roomCode &&
                c.Status == ContractStatus.Active &&
                c.StartDate <= asOf &&
                c.EndDate >= asOf, ct);
        }

        public async Task<Contract?> GetActiveContractByRoomAsync(string roomCode, DateTime asOf, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return null;
            return await _db.Contracts.FirstOrDefaultAsync(c =>
                c.RoomCode == roomCode &&
                c.Status == ContractStatus.Active &&
                c.StartDate <= asOf &&
                c.EndDate >= asOf, ct);
        }

        public async Task MarkNeedsAddendumAsync(string roomCode, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return;

            var list = await _db.Contracts
                .Where(c => c.RoomCode == roomCode && c.Status == ContractStatus.Active)
                .ToListAsync(ct);

            if (list.Count == 0) return;

            foreach (var c in list)
            {
                c.NeedsAddendum = true;
                c.UpdatedAt = DateTime.UtcNow;
                _db.Contracts.Update(c);
            }
            await _db.SaveChangesAsync(ct);
        }
    }
}