using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        // ================= BIKE PRICE HELPERS =================
        private async Task<decimal> ComputeBikePriceForRoomAsync(string roomCode, CancellationToken ct = default)
        {
            var room = await _db.Rooms.FirstOrDefaultAsync(r => r.RoomCode == roomCode, ct);
            if (room == null) return 0m;

            var activeBikeCount = await _db.RoomOccupancies
                .Where(o => o.RoomId == room.IdRoom && o.MoveOutDate == null)
                .SumAsync(o => o.BikeCount, ct);

            return RoomCharge.CalculateBikePrice(room, activeBikeCount);
        }

        private async Task SetBikePriceAsync(RoomCharge rc, CancellationToken ct = default)
        {
            rc.BikePrice = await ComputeBikePriceForRoomAsync(rc.RoomCode, ct);
        }

        // ================= CYCLES =================
        public async Task<PaymentCycle?> GetCycleAsync(int year, int month)
        {
            var cycle = await _db.PaymentCycles
                .Include(c => c.RoomCharges).ThenInclude(rc => rc.Fees)
                .Include(c => c.RoomCharges).ThenInclude(rc => rc.Payments)
                .FirstOrDefaultAsync(c => c.Year == year && c.Month == month);

            if (cycle?.RoomCharges != null)
            {
                foreach (var rc in cycle.RoomCharges)
                    await SetBikePriceAsync(rc);
                _db.RoomCharges.UpdateRange(cycle.RoomCharges);
                await _db.SaveChangesAsync();
            }
            return cycle;
        }

        public async Task<List<PaymentCycle>> GetRecentCyclesAsync(int count = 12)
        {
            return await _db.PaymentCycles
                .OrderByDescending(c => c.Year)
                .ThenByDescending(c => c.Month)
                .Take(count)
                .ToListAsync();
        }

        public async Task<PaymentCycle> CreateCycleAsync(int year, int month)
        {
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

            // Seed from active contracts or active occupancies
            var activeContracts = await _db.Contracts
                .Where(c => c.Status == ContractStatus.Active)
                .ToListAsync();

            if (activeContracts.Any())
            {
                foreach (var c in activeContracts)
                {
                    if (await _db.RoomCharges.AnyAsync(r => r.CycleId == cycle.CycleId && r.RoomCode == c.RoomCode))
                        continue;

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
                    await SetBikePriceAsync(rc);
                    _db.RoomCharges.Add(rc);
                }
            }
            else
            {
                var occupiedRoomCodes = await _db.RoomOccupancies
                    .Where(o => o.MoveOutDate == null)
                    .Select(o => o.Room!.RoomCode)
                    .Distinct()
                    .ToListAsync();

                foreach (var roomCode in occupiedRoomCodes)
                {
                    if (await _db.RoomCharges.AnyAsync(r => r.CycleId == cycle.CycleId && r.RoomCode == roomCode))
                        continue;

                    var rc = new RoomCharge
                    {
                        CycleId = cycle.CycleId,
                        RoomCode = roomCode,
                        BaseRent = 0,
                        Status = PaymentStatus.MissingData,
                        ElectricReading = new ElectricReading(),
                        WaterReading = new WaterReading(),
                        Fees = new List<FeeInstance>(),
                        Payments = new List<PaymentRecord>()
                    };
                    await SetBikePriceAsync(rc);
                    _db.RoomCharges.Add(rc);
                }
            }

            await _db.SaveChangesAsync();
            return (await GetCycleAsync(year, month))!;
        }

        public async Task SeedRoomChargesForCycleAsync(PaymentCycle cycle)
        {
            var activeContracts = await _db.Contracts
                .Where(c => c.Status == ContractStatus.Active)
                .ToListAsync();

            if (activeContracts.Any())
            {
                foreach (var c in activeContracts)
                {
                    if (await _db.RoomCharges.AnyAsync(r => r.CycleId == cycle.CycleId && r.RoomCode == c.RoomCode))
                        continue;

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
                    await SetBikePriceAsync(rc);
                    _db.RoomCharges.Add(rc);
                }
            }
            else
            {
                var occupiedRoomCodes = await _db.RoomOccupancies
                    .Where(o => o.MoveOutDate == null)
                    .Select(o => o.Room!.RoomCode)
                    .Distinct()
                    .ToListAsync();

                foreach (var roomCode in occupiedRoomCodes)
                {
                    if (await _db.RoomCharges.AnyAsync(r => r.CycleId == cycle.CycleId && r.RoomCode == roomCode))
                        continue;

                    var rc = new RoomCharge
                    {
                        CycleId = cycle.CycleId,
                        RoomCode = roomCode,
                        BaseRent = 0,
                        Status = PaymentStatus.MissingData,
                        ElectricReading = new ElectricReading(),
                        WaterReading = new WaterReading(),
                        Fees = new List<FeeInstance>(),
                        Payments = new List<PaymentRecord>()
                    };
                    await SetBikePriceAsync(rc);
                    _db.RoomCharges.Add(rc);
                }
            }

            await _db.SaveChangesAsync();
        }

        public async Task ReseedRoomChargesAsync(string cycleId)
        {
            var cycle = await _db.PaymentCycles.FirstOrDefaultAsync(c => c.CycleId == cycleId);
            if (cycle == null) return;
            await SeedRoomChargesForCycleAsync(cycle);
        }

        public async Task SaveCycleAsync(PaymentCycle cycle)
        {
            _db.PaymentCycles.Update(cycle);
            await _db.SaveChangesAsync();
        }

        // ================= ROOM CHARGES =================
        public async Task<RoomCharge?> GetRoomChargeAsync(string roomChargeId)
        {
            var rc = await _db.RoomCharges
                .Include(r => r.Fees)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(r => r.RoomChargeId == roomChargeId);

            if (rc != null)
            {
                await SetBikePriceAsync(rc);
                _db.RoomCharges.Update(rc);
                await _db.SaveChangesAsync();
            }
            return rc;
        }

        public async Task<List<RoomCharge>> GetRoomChargesForCycleAsync(string cycleId)
        {
            var charges = await _db.RoomCharges
                .Where(r => r.CycleId == cycleId)
                .Include(r => r.Fees)
                .Include(r => r.Payments)
                .OrderBy(r => r.RoomCode)
                .ToListAsync();

            foreach (var rc in charges)
                await SetBikePriceAsync(rc);

            _db.RoomCharges.UpdateRange(charges);
            await _db.SaveChangesAsync();
            return charges;
        }

        public async Task UpdateRoomChargeAsync(RoomCharge rc)
        {
            await SetBikePriceAsync(rc);
            _db.RoomCharges.Update(rc);
            await _db.SaveChangesAsync();
        }

        // ================= PAYMENTS =================
        public async Task AddPaymentRecordAsync(PaymentRecord pr)
        {
            _db.PaymentRecords.Add(pr);
            var rc = await _db.RoomCharges.FirstOrDefaultAsync(x => x.RoomChargeId == pr.RoomChargeId);
            if (rc != null)
            {
                rc.AmountPaid += pr.Amount;
                if (rc.AmountPaid >= rc.TotalDue && rc.TotalDue > 0)
                {
                    rc.Status = PaymentStatus.Paid;
                    rc.PaidAt = pr.PaidAt;
                }
                else if (rc.AmountPaid > 0)
                {
                    rc.Status = PaymentStatus.PartiallyPaid;
                }
                _db.RoomCharges.Update(rc);
            }
            await _db.SaveChangesAsync();
        }

        // ================= FEE TYPES / INSTANCES =================
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

        public async Task<IReadOnlyList<FeeType>> GetFeeTypesAsync(CancellationToken ct = default)
        {
            return await _db.FeeTypes
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync(ct);
        }

        public async Task AddFeeToRoomAsync(string roomChargeId, FeeInstance fee, CancellationToken ct = default)
        {
            var rc = await _db.RoomCharges
                .Include(r => r.Fees)
                .FirstOrDefaultAsync(r => r.RoomChargeId == roomChargeId, ct);

            if (rc == null) return;

            fee.RoomChargeId = roomChargeId;
            var feeAmount = fee.Rate * fee.Quantity;
            var existingTotal = rc.Fees?.Sum(f => f.Rate * f.Quantity) ?? 0m;
            rc.CustomFeesTotal = existingTotal + feeAmount;

            await _db.FeeInstances.AddAsync(fee, ct);
            _db.RoomCharges.Update(rc);
            await _db.SaveChangesAsync(ct);
        }

        public async Task ClearFeesForCycleAsync(string cycleId, CancellationToken ct = default)
        {
            var rcs = await _db.RoomCharges
                .Where(r => r.CycleId == cycleId)
                .Include(r => r.Fees)
                .ToListAsync(ct);

            foreach (var rc in rcs)
            {
                if (rc.Fees?.Count > 0)
                {
                    _db.FeeInstances.RemoveRange(rc.Fees);
                    rc.CustomFeesTotal = 0m;
                    _db.RoomCharges.Update(rc);
                }
            }
            await _db.SaveChangesAsync(ct);
        }

        public async Task SaveFeeTypesAsync(IEnumerable<FeeType> feeTypes, CancellationToken ct = default)
        {
            var incoming = feeTypes.ToList();
            var existing = await _db.FeeTypes.ToListAsync(ct);

            foreach (var t in incoming)
            {
                var found = existing.FirstOrDefault(x => x.FeeTypeId == t.FeeTypeId);
                if (found == null)
                    _db.FeeTypes.Add(t);
                else
                    _db.Entry(found).CurrentValues.SetValues(t);
            }

            var toDelete = existing.Where(x => !incoming.Any(i => i.FeeTypeId == x.FeeTypeId)).ToList();
            if (toDelete.Count > 0)
                _db.FeeTypes.RemoveRange(toDelete);

            await _db.SaveChangesAsync(ct);
        }

        public async Task ApplyFeeToAllRoomsAsync(string cycleId, FeeInstance feeTemplate, CancellationToken ct = default)
        {
            var rcs = await _db.RoomCharges
                .Where(r => r.CycleId == cycleId)
                .Include(r => r.Fees)
                .ToListAsync(ct);

            foreach (var rc in rcs)
            {
                var fi = new FeeInstance
                {
                    RoomChargeId = rc.RoomChargeId,
                    FeeTypeId = feeTemplate.FeeTypeId,
                    Name = feeTemplate.Name,
                    Rate = feeTemplate.Rate,
                    Quantity = feeTemplate.Quantity
                };
                await _db.FeeInstances.AddAsync(fi, ct);

                var sumExisting = rc.Fees?.Sum(f => f.Rate * f.Quantity) ?? 0m;
                rc.CustomFeesTotal = sumExisting + (fi.Rate * fi.Quantity);
                _db.RoomCharges.Update(rc);
            }
            await _db.SaveChangesAsync(ct);
        }

        public async Task RemoveFeeFromRoomAsync(string roomChargeId, string feeInstanceId, CancellationToken ct = default)
        {
            var rc = await _db.RoomCharges
                .Include(r => r.Fees)
                .FirstOrDefaultAsync(r => r.RoomChargeId == roomChargeId, ct);
            if (rc == null || rc.Fees == null) return;

            var fee = rc.Fees.FirstOrDefault(f => f.FeeInstanceId == feeInstanceId);
            if (fee == null) return;

            _db.FeeInstances.Remove(fee);

            var remainingTotal = rc.Fees
                .Where(f => f.FeeInstanceId != feeInstanceId)
                .Sum(f => f.Rate * f.Quantity);

            rc.CustomFeesTotal = remainingTotal;
            _db.RoomCharges.Update(rc);
            await _db.SaveChangesAsync(ct);
        }

        public async Task ApplyFeeTypeToAllExistingCyclesAsync(FeeType ft, CancellationToken ct = default)
        {
            var cycles = await _db.PaymentCycles
                .Include(c => c.RoomCharges).ThenInclude(rc => rc.Fees)
                .ToListAsync(ct);

            foreach (var cycle in cycles)
            {
                foreach (var rc in cycle.RoomCharges)
                {
                    if (rc.Fees.Any(f => f.FeeTypeId == ft.FeeTypeId)) continue;

                    var fi = new FeeInstance
                    {
                        RoomChargeId = rc.RoomChargeId,
                        FeeTypeId = ft.FeeTypeId,
                        Name = ft.Name,
                        Rate = ft.DefaultRate,
                        Quantity = 1
                    };
                    await _db.FeeInstances.AddAsync(fi, ct);

                    var sumExisting = rc.Fees.Sum(f => f.Rate * f.Quantity);
                    rc.CustomFeesTotal = sumExisting + (fi.Rate * fi.Quantity);
                    _db.RoomCharges.Update(rc);
                }
            }
            await _db.SaveChangesAsync(ct);
        }

        public async Task RemoveFeeTypeFromAllCyclesAsync(string feeTypeId, CancellationToken ct = default)
        {
            var cycles = await _db.PaymentCycles
                .Include(c => c.RoomCharges).ThenInclude(rc => rc.Fees)
                .ToListAsync(ct);

            foreach (var cycle in cycles)
            {
                foreach (var rc in cycle.RoomCharges)
                {
                    if (rc.Fees == null || rc.Fees.Count == 0) continue;
                    var toRemove = rc.Fees.Where(f => f.FeeTypeId == feeTypeId).ToList();
                    if (toRemove.Count == 0) continue;

                    _db.FeeInstances.RemoveRange(toRemove);

                    var remainingTotal = rc.Fees
                        .Where(f => f.FeeTypeId != feeTypeId)
                        .Sum(f => f.Rate * f.Quantity);

                    rc.CustomFeesTotal = remainingTotal;
                    _db.RoomCharges.Update(rc);
                }
            }
            await _db.SaveChangesAsync(ct);
        }

        public async Task RemoveFeeTypeFromCycleAsync(string feeTypeId, string cycleId, CancellationToken ct = default)
        {
            var rcList = await _db.RoomCharges
                .Where(r => r.CycleId == cycleId)
                .Include(r => r.Fees)
                .ToListAsync(ct);

            foreach (var rc in rcList)
            {
                if (rc.Fees == null || rc.Fees.Count == 0) continue;
                var toRemove = rc.Fees.Where(f => f.FeeTypeId == feeTypeId).ToList();
                if (toRemove.Count == 0) continue;

                _db.FeeInstances.RemoveRange(toRemove);

                var remainingTotal = rc.Fees
                    .Where(f => f.FeeTypeId != feeTypeId)
                    .Sum(f => f.Rate * f.Quantity);

                rc.CustomFeesTotal = remainingTotal;
                _db.RoomCharges.Update(rc);
            }
            await _db.SaveChangesAsync(ct);
        }

        // ================= BIKE PRICE PUBLIC =================
        public async Task<bool> RecalculateBikePriceAsync(string roomChargeId, CancellationToken ct = default)
        {
            var rc = await _db.RoomCharges.FirstOrDefaultAsync(r => r.RoomChargeId == roomChargeId, ct);
            if (rc == null) return false;
            var newPrice = await ComputeBikePriceForRoomAsync(rc.RoomCode, ct);
            if (rc.BikePrice == newPrice) return false;
            rc.BikePrice = newPrice;
            _db.RoomCharges.Update(rc);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<int> RecalculateBikePricesForCycleAsync(string cycleId, CancellationToken ct = default)
        {
            var charges = await _db.RoomCharges.Where(r => r.CycleId == cycleId).ToListAsync(ct);
            var changed = 0;
            foreach (var rc in charges)
            {
                var newPrice = await ComputeBikePriceForRoomAsync(rc.RoomCode, ct);
                if (rc.BikePrice != newPrice)
                {
                    rc.BikePrice = newPrice;
                    _db.RoomCharges.Update(rc);
                    changed++;
                }
            }
            if (changed > 0) await _db.SaveChangesAsync(ct);
            return changed;
        }

        public async Task<int> BackfillAllBikePricesAsync(CancellationToken ct = default)
        {
            var charges = await _db.RoomCharges.ToListAsync(ct);
            var changed = 0;
            foreach (var rc in charges)
            {
                var newPrice = await ComputeBikePriceForRoomAsync(rc.RoomCode, ct);
                if (rc.BikePrice != newPrice)
                {
                    rc.BikePrice = newPrice;
                    _db.RoomCharges.Update(rc);
                    changed++;
                }
            }
            if (changed > 0) await _db.SaveChangesAsync(ct);
            return changed;
        }
    }
}