using System;
using System.Linq;
using System.Threading.Tasks;
using AMS.Data;
using AMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage;

namespace AMS.Services
{
    public class RoomOccupancyAdminService : IRoomOccupancyAdminService
    {
        private readonly IDbContextFactory<AMSDbContext> _factory;

        public RoomOccupancyAdminService(IDbContextFactory<AMSDbContext> factory)
        {
            _factory = factory;
        }

        public async Task EndAllActiveOccupanciesForRoomAsync(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode)) return;

            await using var db = await _factory.CreateDbContextAsync();

            var room = await db.Rooms
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoomCode == roomCode);

            if (room == null) return;

            var roomId = room.IdRoom;

            // All active occupancies for this room
            var activeOccs = await db.RoomOccupancies
                .Where(o => o.RoomId == roomId && o.MoveOutDate == null)
                .ToListAsync();

            if (activeOccs.Count == 0)
            {
                // Clear any emergency contact preference key regardless
                var prefKey = $"room:{roomId}:emergencyContactOccId";
                if (Preferences.ContainsKey(prefKey)) Preferences.Remove(prefKey);
                return;
            }

            var tenantIds = activeOccs.Select(o => o.TenantId).Distinct().ToList();

            // Delete all bikes in this room for those tenants (EF Core 7+)
            try
            {
                await db.Bikes
                    .Where(b => b.RoomId == roomId && b.OwnerId != null && tenantIds.Contains(b.OwnerId.Value))
                    .ExecuteDeleteAsync();
            }
            catch
            {
                // Fallback for older EF versions
                var bikes = await db.Bikes
                    .Where(b => b.RoomId == roomId && b.OwnerId != null && tenantIds.Contains(b.OwnerId.Value))
                    .ToListAsync();
                db.Bikes.RemoveRange(bikes);
                await db.SaveChangesAsync();
            }

            // End occupancies + clear bike counts
            foreach (var occ in activeOccs)
            {
                occ.MoveOutDate = DateTime.Today;
                occ.BikeCount = 0;
            }

            await db.SaveChangesAsync();

            // Clear emergency contact preference for this room
            var key = $"room:{roomId}:emergencyContactOccId";
            if (Preferences.ContainsKey(key)) Preferences.Remove(key);
        }
    }
}