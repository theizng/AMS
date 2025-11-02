using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace AMS.Models
{
    public class Tenant
    {
        public int IdTenant { get; set; }

        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string IdCardNumber { get; set; } = "";
        public DateTime DateOfBirth { get; set; }
        public string PermanentAddress { get; set; } = "";

        // Legacy/simple link (optional if you rely on RoomOccupancy)
        public int? RoomId { get; set; }
        public DateTime MoveInDate { get; set; }
        public DateTime? MoveOutDate { get; set; }

        public decimal MonthlyRent { get; set; }
        public decimal DepositAmount { get; set; }
        public string? ContractUrl { get; set; }

        public Room? Room { get; set; }

        // Single source for emergency contacts
        public string EmergencyContactsJson { get; set; } = "[]";

        public string Notes { get; set; } = "";
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = "";
        public string UpdatedBy { get; set; } = "";
        public bool IsActive { get; set; } = true;

        public ICollection<RoomOccupancy>? RoomOccupancies { get; set; }

        // Optional nav so EF can map inverse; not used for counting in UI
        public ICollection<Bike>? Bikes { get; set; }

        // Helpers for UI (NotMapped)

        [NotMapped]
        public List<string> EmergencyContacts
        {
            get
            {
                try
                {
                    return string.IsNullOrWhiteSpace(EmergencyContactsJson)
                        ? new List<string>()
                        : (JsonSerializer.Deserialize<List<string>>(EmergencyContactsJson) ?? new List<string>());
                }
                catch { return new List<string>(); }
            }
            set => EmergencyContactsJson = JsonSerializer.Serialize(value ?? new List<string>());
        }

        [NotMapped]
        public string EmergencyContactsText
        {
            get => string.Join('\n', EmergencyContacts);
            set
            {
                if (string.IsNullOrWhiteSpace(value)) { EmergencyContacts = new List<string>(); return; }
                var items = value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(s => s.Trim())
                                 .Where(s => s.Length > 0)
                                 .ToList();
                EmergencyContacts = items;
            }
        }

        [NotMapped]
        public string CurrentRoomDisplay
        {
            get
            {
                // Prefer active occupancy
                var occ = RoomOccupancies?.FirstOrDefault(o => o.MoveOutDate == null);
                if (occ?.Room?.RoomCode is string code && !string.IsNullOrEmpty(code))
                    return code;

                // Fallback to direct Room nav if loaded
                if (!string.IsNullOrEmpty(Room?.RoomCode))
                    return Room!.RoomCode!;

                // Last fallback
                return RoomId?.ToString() ?? "";
            }
        }

        [NotMapped]
        public int ActiveBikeCount
        {
            get
            {
                var occ = RoomOccupancies?.FirstOrDefault(o => o.MoveOutDate == null);
                return occ?.BikeCount ?? 0;
            }
        }
    }
}