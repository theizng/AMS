using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AMS.Data;
using AMS.Models;
using AMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AMS.Services
{
    public class RoomsEfRepository : IRoomsRepository
    {
        private readonly IDbContextFactory<AMSDbContext> _factory;
        private readonly IContractsRepository _contractsRepo;

        public RoomsEfRepository(IDbContextFactory<AMSDbContext> factory, IContractsRepository contractsRepo)
        {
            _factory = factory;
            _contractsRepo = contractsRepo;
        }

        public async Task<Room?> GetByRoomCodeAsync(string roomCode, CancellationToken ct = default)
        {
            await using var db = await _factory.CreateDbContextAsync(ct);
            return await db.Rooms
                .Include(r => r.House)
                .Include(r => r.RoomOccupancies).ThenInclude(ro => ro.Tenant)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoomCode == roomCode, ct);
        }

        public async Task<IReadOnlyList<Room>> GetAllAsync(bool includeInactive = true, CancellationToken ct = default)
        {
            await using var db = await _factory.CreateDbContextAsync(ct);
            var q = db.Rooms.AsNoTracking()
                .Include(r => r.House)
                .Include(r => r.RoomOccupancies).ThenInclude(ro => ro.Tenant)
                .AsQueryable();

            if (!includeInactive)
                q = q.Where(r => r.RoomStatus != Room.Status.Inactive);

            return await q.ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Room>> GetAvailableRoomsForContractAsync(DateTime asOf, CancellationToken ct = default)
        {
            await using var db = await _factory.CreateDbContextAsync(ct);

            var rooms = await db.Rooms
                .AsNoTracking()
                .Include(r => r.House)
                .Include(r => r.RoomOccupancies).ThenInclude(ro => ro.Tenant)
                .Where(r => r.RoomStatus != Room.Status.Inactive)
                .ToListAsync(ct);

            var available = new List<Room>();
            foreach (var r in rooms)
            {
                var hasActiveContract = await _contractsRepo.RoomHasActiveContractAsync(r.RoomCode!, asOf, ct);
                var hasActiveTenant = r.RoomOccupancies != null && r.RoomOccupancies.Any(o => o.MoveOutDate == null);
                if (!hasActiveContract && hasActiveTenant)
                    available.Add(r);
            }

            return available
                .OrderBy(r => r.House!.Address)
                .ThenBy(r => r.RoomCode)
                .ToList();
        }
    }
}