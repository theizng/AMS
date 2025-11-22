using AMS.Models;

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
        Task<IReadOnlyList<FeeType>> GetFeeTypesAsync(System.Threading.CancellationToken ct = default);
        Task AddFeeToRoomAsync(string roomChargeId, FeeInstance fee, System.Threading.CancellationToken ct = default);
        Task ClearFeesForCycleAsync(string cycleId, System.Threading.CancellationToken ct = default);
        Task SaveFeeTypesAsync(System.Collections.Generic.IEnumerable<FeeType> feeTypes, System.Threading.CancellationToken ct = default);
        Task ApplyFeeToAllRoomsAsync(string cycleId, FeeInstance feeTemplate, System.Threading.CancellationToken ct = default);
        Task RemoveFeeFromRoomAsync(string roomChargeId, string feeInstanceId, System.Threading.CancellationToken ct = default);

        Task AddPaymentRecordAsync(PaymentRecord pr);
        Task SaveCycleAsync(PaymentCycle cycle);

        // NEW bulk apply/remove logic
        Task ApplyFeeTypeToAllExistingCyclesAsync(FeeType ft, System.Threading.CancellationToken ct = default);
        Task RemoveFeeTypeFromAllCyclesAsync(string feeTypeId, System.Threading.CancellationToken ct = default);
        Task RemoveFeeTypeFromCycleAsync(string feeTypeId, string cycleId, System.Threading.CancellationToken ct = default);
    }
}