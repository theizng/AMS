using AMS.Data;
using AMS.Models;
using AMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AMS.Services
{
    public class PaymentsRepository : IPaymentsRepository
    {
        private readonly AMSDbContext _db;

        public PaymentsRepository(AMSDbContext db) => _db = db;

        public async Task<PaymentCycle?> GetCycleAsync(int year, int month)
        {
            return await _db.PaymentCycles
                .Include(c => c.RoomCharges)
                    .ThenInclude(rc => rc.Fees)
                .Include(c => c.RoomCharges)
                    .ThenInclude(rc => rc.Payments)
                .FirstOrDefaultAsync(c => c.Year == year && c.Month == month);
        }

        public async Task<PaymentCycle> CreateCycleAsync(int year, int month)
        {
            // If exists, return existing
            var existing = await GetCycleAsync(year, month);
            if (existing != null) return existing;

            var cycle = new PaymentCycle
            {
                Year = year,
                Month = month,
                CreatedAt = DateTime.UtcNow,
                Closed = false
            };
            _db.PaymentCycles.Add(cycle);
            await _db.SaveChangesAsync();

            // Seed RoomCharges from active contracts
            var activeContracts = await _db.Contracts
                .Where(c => c.Status == ContractStatus.Active)
                .ToListAsync();

            foreach (var c in activeContracts)
            {
                var rc = new RoomCharge
                {
                    CycleId = cycle.CycleId,
                    RoomCode = c.RoomCode,
                    BaseRent = c.RentAmount,
                    Status = PaymentStatus.MissingData,
                    ElectricReading = new ElectricReading(),
                    WaterReading = new WaterReading(),
                    Fees = new List<FeeInstance>(),
                    Payments = new List<PaymentRecord>()
                };
                _db.RoomCharges.Add(rc);
            }

            await _db.SaveChangesAsync();
            // Reload with children
            return (await GetCycleAsync(year, month))!;
        }

        public async Task SaveCycleAsync(PaymentCycle cycle)
        {
            _db.PaymentCycles.Update(cycle);
            await _db.SaveChangesAsync();
        }

        public async Task<List<PaymentCycle>> GetRecentCyclesAsync(int count = 12)
        {
            return await _db.PaymentCycles
                .OrderByDescending(c => c.Year)
                .ThenByDescending(c => c.Month)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<FeeType>> GetFeeTypesAsync()
            => await _db.FeeTypes.Where(f => f.Active).OrderBy(f => f.Name).ToListAsync();

        public async Task<FeeType> AddFeeTypeAsync(FeeType ft)
        {
            _db.FeeTypes.Add(ft);
            await _db.SaveChangesAsync();
            return ft;
        }

        public async Task UpdateFeeTypeAsync(FeeType ft)
        {
            _db.FeeTypes.Update(ft);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateRoomChargeAsync(RoomCharge rc)
        {
            _db.RoomCharges.Update(rc);
            await _db.SaveChangesAsync();
        }

        public async Task<RoomCharge?> GetRoomChargeAsync(string roomChargeId)
        {
            return await _db.RoomCharges
                .Include(r => r.Fees)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(r => r.RoomChargeId == roomChargeId);
        }

        public async Task<List<RoomCharge>> GetRoomChargesForCycleAsync(string cycleId)
        {
            return await _db.RoomCharges
                .Where(r => r.CycleId == cycleId)
                .Include(r => r.Fees)
                .Include(r => r.Payments)
                .OrderBy(r => r.RoomCode)
                .ToListAsync();
        }

        public async Task AddPaymentRecordAsync(PaymentRecord pr)
        {
            _db.PaymentRecords.Add(pr);
            // Also update RoomCharge aggregates and status
            var rc = await _db.RoomCharges.FirstOrDefaultAsync(x => x.RoomChargeId == pr.RoomChargeId);
            if (rc != null)
            {
                rc.AmountPaid += pr.Amount;
                if (rc.AmountPaid >= rc.TotalDue)
                {
                    rc.Status = PaymentStatus.Paid;
                    rc.PaidAt = pr.PaidAt;
                }
                else
                {
                    rc.Status = PaymentStatus.PartiallyPaid;
                }
                _db.RoomCharges.Update(rc);
            }
            await _db.SaveChangesAsync();
        }
    }
}