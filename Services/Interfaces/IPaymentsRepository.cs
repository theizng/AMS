using AMS.Models;

namespace AMS.Services.Interfaces
{
    public interface IPaymentsRepository
    {
        
        //===============================PAYMENT CYCLE =========================================
        //Lấy ra cụ thể 1 chu kỳ thanh toán (PaymentCycle model)
        Task<PaymentCycle?> GetCycleAsync(int year, int month);
        Task<List<PaymentCycle>> GetRecentCyclesAsync(int count = 12);
        Task<PaymentCycle> CreateCycleAsync(int year, int month);


        
        //Tải danh sách tình trạng thanh toán cho các phòng có hợp đồng đang hoạt động
        //==============================ROOM CHARGE/============================================
        Task ReseedRoomChargesAsync(string cycleId);
        Task<RoomCharge?> GetRoomChargeAsync(string roomChargeId);
        Task<List<RoomCharge>> GetRoomChargesForCycleAsync(string cycleId);
        Task UpdateRoomChargeAsync(RoomCharge rc);
        //===========================FEE TYPE, FEE ENTITY (Model) =============================
        Task<List<FeeType>> GetFeeTypesAsync();
        Task<FeeType> AddFeeTypeAsync(FeeType ft);
        Task UpdateFeeTypeAsync(FeeType ft);
        Task<IReadOnlyList<FeeType>> GetFeeTypesAsync(CancellationToken ct = default);
        Task AddFeeToRoomAsync(string roomChargeId, FeeInstance fee, CancellationToken ct = default);
        Task ClearFeesForCycleAsync(string cycleId, CancellationToken ct = default);
        Task SaveFeeTypesAsync(IEnumerable<FeeType> feeTypes, CancellationToken ct = default);
        Task ApplyFeeToAllRoomsAsync(string cycleId, FeeInstance feeTemplate, CancellationToken ct = default);


        //Thêm ghi nhận thanh toán (PaymentRecord model)
        //=============================PAYMENT RECORD=======================================
        Task AddPaymentRecordAsync(PaymentRecord pr);
        Task SaveCycleAsync(PaymentCycle cycle);


    }
}