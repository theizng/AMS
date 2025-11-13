using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AMS.Models;

namespace AMS.Services.Interfaces
{
    public interface IContractAddendumService
    {
        /// <summary>
        /// Creates an addendum based on current room occupancies vs parent contract tenants.
        /// Generates PDF, persists the addendum, updates the parent contract Tenants snapshot
        /// to the new tenants, and clears NeedsAddendum/AddendumNotifiedAt on parent.
        /// Returns the created addendum.
        /// </summary>
        Task<ContractAddendum> CreateAddendumFromRoomChangeAsync(
            Contract parent,
            string? reason,
            DateTime? effectiveDate,
            LandlordInfo landlord,
            CancellationToken ct = default);

        Task<IReadOnlyList<ContractAddendum>> GetAddendumsAsync(string parentContractId, CancellationToken ct = default);
    }
}