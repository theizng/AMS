using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMS.Models
{
    public enum ContractStatus
    {
        Draft = 0,
        Active = 1,
        Terminated = 2,
        Expired = 3
    }

    public class ContractTenant
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
    }

    public class Contract
    {
        public string ContractId { get; set; } = Guid.NewGuid().ToString("N");
        public string? ContractNumber { get; set; }

        public string RoomCode { get; set; } = "";
        public string HouseAddress { get; set; } = "";

        public string TenantsJson { get; set; } = "[]";

        [NotMapped]
        public List<ContractTenant> Tenants
        {
            get
            {
                try
                {
                    return string.IsNullOrWhiteSpace(TenantsJson)
                        ? new List<ContractTenant>()
                        : JsonSerializer.Deserialize<List<ContractTenant>>(TenantsJson) ?? new List<ContractTenant>();
                }
                catch
                {
                    return new List<ContractTenant>();
                }
            }
            set
            {
                TenantsJson = JsonSerializer.Serialize(value ?? new List<ContractTenant>());
            }
        }

        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(12);

        public decimal RentAmount { get; set; }
        public int DueDay { get; set; } = 5;
        public string PaymentMethods { get; set; } = "";
        public string LateFeePolicy { get; set; } = "";
        public decimal SecurityDeposit { get; set; }
        public int DepositReturnDays { get; set; } = 7;

        public int MaxOccupants { get; set; }
        public int MaxBikeAllowance { get; set; }

        public string PropertyDescription { get; set; } = "";
        public string? PdfUrl { get; set; }

        public ContractStatus Status { get; set; } = ContractStatus.Draft;
        public bool NeedsAddendum { get; set; } = false;

        // NEW: Timestamp to avoid email spam. Set when NeedsAddendum flips to true and email is sent.
        public DateTime? AddendumNotifiedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // NEW: Children addendums
        public List<ContractAddendum> Addendums { get; set; } = new();

        [NotMapped]
        public bool IsActiveNow => Status == ContractStatus.Active && DateTime.Today <= EndDate && DateTime.Today >= StartDate;

        [NotMapped]
        public int DaysToEnd => (EndDate - DateTime.Today).Days;

        public bool IsExpiringSoon(int days = 30) => Status == ContractStatus.Active && DaysToEnd >= 0 && DaysToEnd <= days;

        [NotMapped]
        public string StatusVi => Status switch
        {
            ContractStatus.Draft => "Bản nháp",
            ContractStatus.Active => "Hiệu lực",
            ContractStatus.Terminated => "Chấm dứt",
            ContractStatus.Expired => "Hết hạn",
            _ => "Bản nháp"
        };
    }
} 