using AMS.Models;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AMS.Services.Interfaces
{
    public interface IPaymentsRepository
    {
        Task<PaymentCycle?> GetCycleAsync(int year, int month);
        Task<List<PaymentCycle>> GetRecentCyclesAsync(int count = 12);
        Task<PaymentCycle> CreateCycleAsync(int year, int month);
        Task ReseedRoomChargesAsync(string cycleId);
        Task<RoomCharge?> GetRoomChargeAsync(string roomChargeId);
        Task<List<RoomCharge>> GetRoomChargesForCycleAsync(string cycleId);
        Task UpdateRoomChargeAsync(RoomCharge rc);

        Task<List<FeeType>> GetFeeTypesAsync();
        Task<FeeType> AddFeeTypeAsync(FeeType ft);
        Task UpdateFeeTypeAsync(FeeType ft);
        Task<IReadOnlyList<FeeType>> GetFeeTypesAsync(CancellationToken ct = default);
        Task AddFeeToRoomAsync(string roomChargeId, FeeInstance fee, CancellationToken ct = default);
        Task ClearFeesForCycleAsync(string cycleId, CancellationToken ct = default);
        Task SaveFeeTypesAsync(IEnumerable<FeeType> feeTypes, CancellationToken ct = default);
        Task ApplyFeeToAllRoomsAsync(string cycleId, FeeInstance feeTemplate, CancellationToken ct = default);
        Task RemoveFeeFromRoomAsync(string roomChargeId, string feeInstanceId, CancellationToken ct = default);

        Task AddPaymentRecordAsync(PaymentRecord pr);
        Task SaveCycleAsync(PaymentCycle cycle);

        Task ApplyFeeTypeToAllExistingCyclesAsync(FeeType ft, CancellationToken ct = default);
        Task RemoveFeeTypeFromAllCyclesAsync(string feeTypeId, CancellationToken ct = default);
        Task RemoveFeeTypeFromCycleAsync(string feeTypeId, string cycleId, CancellationToken ct = default);

        // NEW bike fee recalculation helpers
        Task<bool> RecalculateBikePriceAsync(string roomChargeId, CancellationToken ct = default);
        Task<int> RecalculateBikePricesForCycleAsync(string cycleId, CancellationToken ct = default);
        Task<int> BackfillAllBikePricesAsync(CancellationToken ct = default);
    }
}