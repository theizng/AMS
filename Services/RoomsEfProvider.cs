using AMS.Models;
using AMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using AMS.Data;
namespace AMS.Services
{
    // Reads ALL rooms from your database and maps to RoomInfo for syncing.
    public class RoomsEfProvider : IRoomsProvider
    {
        private readonly IDbContextFactory<AMSDbContext> _factory;

        public RoomsEfProvider(IDbContextFactory<AMSDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyList<RoomInfo>> GetRoomsAsync(bool includeInactive = true, CancellationToken ct = default)
        {
            await using var db = await _factory.CreateDbContextAsync(ct);

            var query = db.Set<Room>()
                .AsNoTracking()
                .Include(r => r.House)
                .AsQueryable();

            if (!includeInactive)
                query = query.Where(r => r.RoomStatus != Room.Status.Inactive);

            var list = await query.ToListAsync(ct);

            // Map Room -> RoomInfo (only fields used by the script now)
            var mapped = list.Select(r =>
            {
                // NOTE: if your House uses a different property for address, change "Address" below accordingly
                var houseAddress = r.House?.Address; // e.g., change to r.House?.HouseAddress if needed

                // Active = false only when RoomStatus == Inactive
                bool active = r.RoomStatus != Room.Status.Inactive;

                return new RoomInfo(
                    RoomCode: r.RoomCode ?? string.Empty,
                    HouseAddress: houseAddress ?? string.Empty,
                    TenantName: null,
                    TenantPhone: null,
                    Active: active
                );
            })
            // Keep deterministic order
            .OrderBy(x => x.RoomCode)
            .ToList();

            return mapped;
        }
    }
}