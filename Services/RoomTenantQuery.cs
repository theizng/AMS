using AMS.Data;
using AMS.Models;
using AMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace AMS.Services
{
    public class RoomTenantQuery : IRoomTenantQuery
    {
        private readonly AMSDbContext _db;
        public RoomTenantQuery(AMSDbContext db) => _db = db;

        public async Task<RoomTenantInfo> GetForRoomAsync(string roomCode)
        {
            var info = new RoomTenantInfo();

            var room = await _db.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.RoomCode == roomCode);
            if (room == null) return info;

            // Active occupancies (no MoveOutDate)
            var occs = await _db.RoomOccupancies
                .AsNoTracking()
                .Where(o => o.RoomId == room.IdRoom && o.MoveOutDate == null)
                .Include(o => o.Tenant)
                .ToListAsync();

            foreach (var o in occs)
            {
                if (o.Tenant != null)
                {
                    if (!string.IsNullOrWhiteSpace(o.Tenant.FullName)) info.Names.Add(o.Tenant.FullName);
                    if (!string.IsNullOrWhiteSpace(o.Tenant.PhoneNumber)) info.Phones.Add(o.Tenant.PhoneNumber);
                    // If your Tenant has Email, add here; else you pass emails from another source
                    var emailProp = o.Tenant.GetType().GetProperty("Email");
                    var emailVal = emailProp?.GetValue(o.Tenant)?.ToString();
                    if (!string.IsNullOrWhiteSpace(emailVal)) info.Emails.Add(emailVal!);
                }
            }

            // Contract by room code (status Active if you use that)
            var ctr = await _db.Contracts.AsNoTracking()
                .Where(c => c.RoomCode == roomCode && c.Status == ContractStatus.Active)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            if (ctr != null)
            {
                info.ContractNumber = ctr.ContractNumber;
                info.ContractStartDate = ctr.CreatedAt;
            }

            return info;
        }
    }
}