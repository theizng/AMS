using AMS.Data;
using AMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AMS.Services
{
    // EF-backed implementation that returns active tenants for a room.
    public class RoomOccupancyEfProvider : IRoomOccupancyProvider
    {
        private readonly IDbContextFactory<AMSDbContext> _factory;

        public RoomOccupancyEfProvider(IDbContextFactory<AMSDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyList<RoomTenantDto>> GetTenantsForRoomAsync(string roomCode, CancellationToken ct = default)
        {
            await using var db = await _factory.CreateDbContextAsync(ct);

            // Find the room id
            var room = await db.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.RoomCode == roomCode, ct);
            if (room == null) return new List<RoomTenantDto>();

            var occs = await db.RoomOccupancies
                .AsNoTracking()
                .Where(o => o.RoomId == room.IdRoom && o.MoveOutDate == null)
                .Include(o => o.Tenant)
                .ToListAsync(ct);

            var result = occs.Select(o => new RoomTenantDto
            {
                Name = o.Tenant?.FullName ?? "",
                Email = o.Tenant?.Email ?? "",
                Phone = o.Tenant?.PhoneNumber ?? ""
            }).ToList();

            return result;
        }
    }
}