using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AMS.Models;

namespace AMS.Services.Interfaces
{
    public interface IContractAddendumService
    {
        // Keep your existing APIs if already used elsewhere
        Task<ContractAddendum> CreateAddendumFromRoomChangeAsync(
            Contract parent, string? reason, DateTime? effectiveDate, LandlordInfo landlord, CancellationToken ct = default);

        Task<ContractAddendum> CreateAddendumWithSnapshotsAsync(
            Contract parent,
            ContractSnapshot oldSnapshot,
            ContractSnapshot newSnapshot,
            string? reason,
            DateTime? effectiveDate,
            LandlordInfo landlord,
            CancellationToken ct = default);

        // NEW: Single entry-point for the flow you described:
        // - OldSnapshot = current Contract
        // - NewSnapshot = current Contract overwritten by UI changes, and Tenants taken from RoomOccupancy
        Task<ContractAddendum> CreateAddendumFromCurrentContractAndRoomAsync(
            Contract parent,
            ContractSnapshot uiChangesSnapshot,
            string? reason,
            DateTime? effectiveDate,
            LandlordInfo landlord,
            CancellationToken ct = default);

        Task<IReadOnlyList<ContractAddendum>> GetAddendumsAsync(string parentContractId, CancellationToken ct = default);
    }
}