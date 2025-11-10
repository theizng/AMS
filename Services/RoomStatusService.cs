using AMS.Data;
using AMS.Models;
using AMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace AMS.Services
{
    public class RoomStatusService : IRoomStatusService
    {
        private readonly IDbContextFactory<AMSDbContext> _factory;
        public RoomStatusService(IDbContextFactory<AMSDbContext> factory) => _factory = factory;

        public async Task SetRoomOccupiedAsync(string roomCode)
        {
            await using var db = await _factory.CreateDbContextAsync();
            var room = await db.Rooms.FirstOrDefaultAsync(r => r.RoomCode == roomCode);
            if (room == null) return;
            room.RoomStatus = Room.Status.Occupied;
            room.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        public async Task SetRoomAvailableAsync(string roomCode)
        {
            await using var db = await _factory.CreateDbContextAsync();
            var room = await db.Rooms.FirstOrDefaultAsync(r => r.RoomCode == roomCode);
            if (room == null) return;
            // Only revert if not manually set to Maintaining / Inactive
            if (room.RoomStatus == Room.Status.Occupied)
            {
                room.RoomStatus = Room.Status.Available;
                room.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
        }
    }
}