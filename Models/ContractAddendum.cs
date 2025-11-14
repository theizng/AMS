using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace AMS.Models
{
    public class ContractAddendum
    {
        public string AddendumId { get; set; } = Guid.NewGuid().ToString("N");
        public string ParentContractId { get; set; } = "";
        public string? AddendumNumber { get; set; }
        public string? Reason { get; set; }

        // Tenants-only (kept for compatibility)
        public string OldTenantsJson { get; set; } = "[]";
        public string NewTenantsJson { get; set; } = "[]";

        [NotMapped]
        public List<ContractTenant> OldTenants
        {
            get => string.IsNullOrWhiteSpace(OldTenantsJson)
                ? new()
                : (JsonSerializer.Deserialize<List<ContractTenant>>(OldTenantsJson) ?? new());
            set => OldTenantsJson = JsonSerializer.Serialize(value ?? new());
        }

        [NotMapped]
        public List<ContractTenant> NewTenants
        {
            get => string.IsNullOrWhiteSpace(NewTenantsJson)
                ? new()
                : (JsonSerializer.Deserialize<List<ContractTenant>>(NewTenantsJson) ?? new());
            set => NewTenantsJson = JsonSerializer.Serialize(value ?? new());
        }

        // NEW: full snapshots (for reliable history)
        public string OldSnapshotJson { get; set; } = "{}";
        public string NewSnapshotJson { get; set; } = "{}";

        [NotMapped]
        public ContractSnapshot OldSnapshot
        {
            get => string.IsNullOrWhiteSpace(OldSnapshotJson)
                ? new ContractSnapshot()
                : (JsonSerializer.Deserialize<ContractSnapshot>(OldSnapshotJson) ?? new ContractSnapshot());
            set => OldSnapshotJson = JsonSerializer.Serialize(value ?? new ContractSnapshot());
        }

        [NotMapped]
        public ContractSnapshot NewSnapshot
        {
            get => string.IsNullOrWhiteSpace(NewSnapshotJson)
                ? new ContractSnapshot()
                : (JsonSerializer.Deserialize<ContractSnapshot>(NewSnapshotJson) ?? new ContractSnapshot());
            set => NewSnapshotJson = JsonSerializer.Serialize(value ?? new ContractSnapshot());
        }

        public DateTime? EffectiveDate { get; set; }
        public string? PdfUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Contract? Parent { get; set; }
    }
}