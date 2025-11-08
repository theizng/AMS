using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AMS.Models;

namespace AMS.Services.Interfaces
{
    /// <summary>
    /// Repository used by app features (contracts, occupancy, UI) to get full Room data from database.
    /// This is separate from IRoomsProvider which is responsible for syncing a small RoomInfo to Google Sheet.
    /// </summary>
    public interface IRoomsRepository
    {
        Task<Room?> GetByRoomCodeAsync(string roomCode, CancellationToken ct = default);
        Task<IReadOnlyList<Room>> GetAllAsync(bool includeInactive = true, CancellationToken ct = default);
        /// <summary>
        /// Returns rooms that are available to create a new (active) contract as of 'asOf' date.
        /// i.e. no overlapping active contract and optionally other business rules.
        /// </summary>
        Task<IReadOnlyList<Room>> GetAvailableRoomsForContractAsync(DateTime asOf, CancellationToken ct = default);
    }
}