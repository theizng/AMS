using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMS.Services.Interfaces
{
    public interface IRoomTenantQuery
    {
        Task<RoomTenantInfo> GetForRoomAsync(string roomCode);
    }

    public class RoomTenantInfo
    {
        public List<string> Names { get; set; } = new();
        public List<string> Phones { get; set; } = new();
        public List<string> Emails { get; set; } = new();
        public string? ContractNumber { get; set; }
        public DateTime? ContractStartDate { get; set; }
    }
}