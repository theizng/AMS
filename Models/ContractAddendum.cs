using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace AMS.Models
{
    public class ContractAddendum
    {
        // PK: GUID string
        public string AddendumId { get; set; } = Guid.NewGuid().ToString("N");

        // FK to parent contract
        public string ParentContractId { get; set; } = "";

        // Optional: human-readable addendum number
        public string? AddendumNumber { get; set; }

        // Summary/reason for change (optional, for admin note)
        public string? Reason { get; set; }

        // Snapshots of tenants (before/after)
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

        // Effective date of addendum (optional)
        public DateTime? EffectiveDate { get; set; }

        // Generated PDF for this addendum (optional)
        public string? PdfUrl { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Contract? Parent { get; set; }
    }
}