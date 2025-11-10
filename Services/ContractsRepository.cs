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
    // Use a fresh DbContext per call to avoid tracking conflicts.
    public class ContractsRepository : IContractsRepository
    {
        private readonly IDbContextFactory<AMSDbContext> _factory;

        public ContractsRepository(IDbContextFactory<AMSDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyList<Contract>> GetAllAsync(CancellationToken ct = default)
        {
            await using var db = await _factory.CreateDbContextAsync(ct);
            return await db.Contracts
                .AsNoTracking()
                .OrderByDescending(c => c.Status == ContractStatus.Active)
                .ThenBy(c => c.EndDate)
                .ToListAsync(ct);
        }

        public async Task<Contract?> GetByIdAsync(string contractId, CancellationToken ct = default)
        {
            await using var db = await _factory.CreateDbContextAsync(ct);
            return await db.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.ContractId == contractId, ct);
        }

        public async Task CreateAsync(Contract contract, CancellationToken ct = default)
        {
            if (contract == null) throw new ArgumentNullException(nameof(contract));
            await using var db = await _factory.CreateDbContextAsync(ct);

            if (await RoomHasActiveContractAsync(contract.RoomCode, DateTime.Today, ct))
                throw new InvalidOperationException("Room already has an active contract.");

            contract.CreatedAt = DateTime.UtcNow;
            contract.UpdatedAt = DateTime.UtcNow;

            db.Contracts.Add(contract);
            await db.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Contract contract, CancellationToken ct = default)
        {
            if (contract == null) throw new ArgumentNullException(nameof(contract));
            await using var db = await _factory.CreateDbContextAsync(ct);

            contract.UpdatedAt = DateTime.UtcNow;
            db.Attach(contract);
            db.Entry(contract).State = EntityState.Modified;
            await db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(string contractId, CancellationToken ct = default)
        {
            await using var db = await _factory.CreateDbContextAsync(ct);
            var e = await db.Contracts.FirstOrDefaultAsync(x => x.ContractId == contractId, ct);
            if (e == null) return;
            db.Contracts.Remove(e);
            await db.SaveChangesAsync(ct);
        }

        public async Task<bool> RoomHasActiveContractAsync(string roomCode, DateTime asOf, CancellationToken ct = default)
        {
            await using var db = await _factory.CreateDbContextAsync(ct);
            return await db.Contracts.AnyAsync(c =>
                c.RoomCode == roomCode &&
                c.Status == ContractStatus.Active &&
                c.StartDate <= asOf &&
                c.EndDate >= asOf, ct);
        }

        public async Task<Contract?> GetActiveContractByRoomAsync(string roomCode, DateTime asOf, CancellationToken ct = default)
        {
            await using var db = await _factory.CreateDbContextAsync(ct);
            return await db.Contracts.FirstOrDefaultAsync(c =>
                c.RoomCode == roomCode &&
                c.Status == ContractStatus.Active &&
                c.StartDate <= asOf &&
                c.EndDate >= asOf, ct);
        }

        public async Task MarkNeedsAddendumAsync(string roomCode, CancellationToken ct = default)
        {
            await using var db = await _factory.CreateDbContextAsync(ct);
            var list = await db.Contracts.Where(c => c.RoomCode == roomCode && c.Status == ContractStatus.Active).ToListAsync(ct);
            if (list.Count == 0) return;
            foreach (var c in list)
            {
                c.NeedsAddendum = true;
                c.UpdatedAt = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(ct);
        }
    }
}