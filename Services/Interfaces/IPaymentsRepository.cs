using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AMS.Models;

namespace AMS.Services.Interfaces
{
    public interface IPaymentsRepository
    {
        Task<PaymentCycle?> GetCycleAsync(int year, int month);
        Task<PaymentCycle> CreateCycleAsync(int year, int month);
        Task SaveCycleAsync(PaymentCycle cycle);

        Task<List<PaymentCycle>> GetRecentCyclesAsync(int count = 12);

        Task<List<FeeType>> GetFeeTypesAsync();
        Task<FeeType> AddFeeTypeAsync(FeeType ft);
        Task UpdateFeeTypeAsync(FeeType ft);

        Task UpdateRoomChargeAsync(RoomCharge rc);
        Task<RoomCharge?> GetRoomChargeAsync(string roomChargeId);
        Task<List<RoomCharge>> GetRoomChargesForCycleAsync(string cycleId);

        Task AddPaymentRecordAsync(PaymentRecord pr);
    }
}