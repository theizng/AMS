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
    // NOTE:
    // - This repository uses a single AMSDbContext instance (_db) injected via DI.
    // - All fee-related methods were updated to use _db (no _factory usage).
    // - We keep existing methods so other pages (e.g., PaymentsViewModel) are not broken.
    public partial class PaymentsRepository : IPaymentsRepository
    {
        private readonly AMSDbContext _db;

        public PaymentsRepository(AMSDbContext db) => _db = db;
        //===============================PAYMENT CYCLE =========================================
        public async Task<PaymentCycle?> GetCycleAsync(int year, int month)
        {
            return await _db.PaymentCycles
                .Include(c => c.RoomCharges).ThenInclude(rc => rc.Fees)
                .Include(c => c.RoomCharges).ThenInclude(rc => rc.Payments)
                .FirstOrDefaultAsync(c => c.Year == year && c.Month == month);
        }
        public async Task ClearFeesForCycleAsync(string cycleId, CancellationToken ct)
        {
            // Load RCs with fees
            var rcs = await _db.RoomCharges
                .Where(r => r.CycleId == cycleId)
                .Include(r => r.Fees)
                .ToListAsync(ct);

            // Remove all fee instances, reset totals
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
        public async Task<IReadOnlyList<PaymentCycle>> GetCyclesAsync()
        {
            return await _db.PaymentCycles
                .OrderByDescending(c => c.Year)
                .ThenByDescending(c => c.Month)
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

            // seed from active contracts
            var activeContracts = await _db.Contracts
                .Where(c => c.Status == ContractStatus.Active)
                .ToListAsync();

            if (activeContracts.Count > 0)
            {
                foreach (var c in activeContracts)
                {
                    if (!await _db.RoomCharges.AnyAsync(r => r.CycleId == cycle.CycleId && r.RoomCode == c.RoomCode))
                    {
                        _db.RoomCharges.Add(new RoomCharge
                        {
                            CycleId = cycle.CycleId,
                            RoomCode = c.RoomCode,
                            BaseRent = c.RentAmount,
                            Status = PaymentStatus.MissingData,
                            ElectricReading = new ElectricReading(),
                            WaterReading = new WaterReading(),
                            Fees = new List<FeeInstance>(),
                            Payments = new List<PaymentRecord>()
                        });
                    }
                }
            }
            else
            {
                // fallback: active occupancies
                var occupiedRoomCodes = await _db.RoomOccupancies
                    .Where(o => o.MoveOutDate == null)
                    .Select(o => o.Room!.RoomCode)
                    .Distinct()
                    .ToListAsync();

                foreach (var roomCode in occupiedRoomCodes)
                {
                    if (!await _db.RoomCharges.AnyAsync(r => r.CycleId == cycle.CycleId && r.RoomCode == roomCode))
                    {
                        _db.RoomCharges.Add(new RoomCharge
                        {
                            CycleId = cycle.CycleId,
                            RoomCode = roomCode,
                            BaseRent = 0,
                            Status = PaymentStatus.MissingData,
                            ElectricReading = new ElectricReading(),
                            WaterReading = new WaterReading(),
                            Fees = new List<FeeInstance>(),
                            Payments = new List<PaymentRecord>()
                        });
                    }
                }
            }

            await _db.SaveChangesAsync();
            return (await GetCycleAsync(year, month))!;
        }
        public async Task SeedRoomChargesForCycleAsync(PaymentCycle cycle)
        {
            // Active contracts
            var activeContracts = await _db.Contracts
                .Where(c => c.Status == ContractStatus.Active)
                .ToListAsync();

            if (activeContracts.Count > 0)
            {
                foreach (var c in activeContracts)
                {
                    if (!await _db.RoomCharges.AnyAsync(r => r.CycleId == cycle.CycleId && r.RoomCode == c.RoomCode))
                    {
                        _db.RoomCharges.Add(new RoomCharge
                        {
                            CycleId = cycle.CycleId,
                            RoomCode = c.RoomCode,
                            BaseRent = c.RentAmount,
                            Status = PaymentStatus.MissingData,
                            ElectricReading = new ElectricReading(),
                            WaterReading = new WaterReading(),
                            Fees = new List<FeeInstance>(),
                            Payments = new List<PaymentRecord>()
                        });
                    }
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
                    if (!await _db.RoomCharges.AnyAsync(r => r.CycleId == cycle.CycleId && r.RoomCode == roomCode))
                    {
                        _db.RoomCharges.Add(new RoomCharge
                        {
                            CycleId = cycle.CycleId,
                            RoomCode = roomCode,
                            BaseRent = 0,
                            Status = PaymentStatus.MissingData,
                            ElectricReading = new ElectricReading(),
                            WaterReading = new WaterReading(),
                            Fees = new List<FeeInstance>(),
                            Payments = new List<PaymentRecord>()
                        });
                    }
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
        public async Task<List<PaymentCycle>> GetRecentCyclesAsync(int count = 12)
        {
            return await _db.PaymentCycles
                .OrderByDescending(c => c.Year)
                .ThenByDescending(c => c.Month)
                .Take(count)
                .ToListAsync();
        }

        //===============================FEE TYPE, FEE ENTITY (Model) =============================
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
        async Task<IReadOnlyList<FeeType>> IPaymentsRepository.GetFeeTypesAsync(CancellationToken ct)
        {
            return await _db.FeeTypes.AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync(ct);
        }
        async Task IPaymentsRepository.SaveFeeTypesAsync(IEnumerable<FeeType> feeTypes, CancellationToken ct)
        {
            var incoming = feeTypes.ToList();
            var existing = await _db.FeeTypes.ToListAsync(ct);

            // Upsert by FeeTypeId
            foreach (var t in incoming)
            {
                var found = existing.FirstOrDefault(x => x.FeeTypeId == t.FeeTypeId);
                if (found == null)
                {
                    _db.FeeTypes.Add(t);
                }
                else
                {
                    _db.Entry(found).CurrentValues.SetValues(t);
                }
            }

            // Delete removed (optional; keep strict with UI save)
            var toDelete = existing.Where(x => !incoming.Any(i => i.FeeTypeId == x.FeeTypeId)).ToList();
            if (toDelete.Count > 0)
                _db.FeeTypes.RemoveRange(toDelete);

            await _db.SaveChangesAsync(ct);
        }
        async Task IPaymentsRepository.AddFeeToRoomAsync(string roomChargeId, FeeInstance fee, CancellationToken ct)
        {
            // Load RoomCharge (and existing fees) first to avoid double counting
            var rc = await _db.RoomCharges
                .Include(r => r.Fees)
                .FirstOrDefaultAsync(r => r.RoomChargeId == roomChargeId, ct);

            if (rc == null) return;

            // Prepare fee
            fee.RoomChargeId = roomChargeId;
            var feeAmount = fee.Rate * fee.Quantity;

            // CustomFeesTotal BEFORE adding new fee + new fee amount
            var existingTotal = rc.Fees?.Sum(f => f.Rate * f.Quantity) ?? 0m;
            rc.CustomFeesTotal = existingTotal + feeAmount;

            await _db.FeeInstances.AddAsync(fee, ct);
            _db.RoomCharges.Update(rc);

            await _db.SaveChangesAsync(ct);
        }
        async Task IPaymentsRepository.ApplyFeeToAllRoomsAsync(string cycleId, FeeInstance feeTemplate, CancellationToken ct)
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
        
        //==============================ROOM CHARGE/============================================
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

        // ===== Interface methods with CancellationToken (use _db, pass ct) =====
        //========================
        public async Task RemoveFeeFromRoomAsync(string roomChargeId, string feeInstanceId, CancellationToken ct = default)
        {
            var rc = await _db.RoomCharges
                .Include(r => r.Fees)
                .FirstOrDefaultAsync(r => r.RoomChargeId == roomChargeId, ct);
            if (rc == null || rc.Fees == null) return;

            var fee = rc.Fees.FirstOrDefault(f => f.FeeInstanceId == feeInstanceId);
            if (fee == null) return;

            var amount = fee.Rate * fee.Quantity;
            _db.FeeInstances.Remove(fee);

            // Recalculate total from remaining (avoid subtract drift)
            var remainingTotal = rc.Fees.Where(f => f.FeeInstanceId != feeInstanceId)
                .Sum(f => f.Rate * f.Quantity);
            rc.CustomFeesTotal = remainingTotal;

            _db.RoomCharges.Update(rc);
            await _db.SaveChangesAsync(ct);
        }

        public async Task ApplyFeeTypeToAllExistingCyclesAsync(FeeType ft, CancellationToken ct = default)
        {
            // Load all cycles with room charges + fees
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

                    var remainingTotal = rc.Fees.Where(f => f.FeeTypeId != feeTypeId)
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

                var remainingTotal = rc.Fees.Where(f => f.FeeTypeId != feeTypeId)
                    .Sum(f => f.Rate * f.Quantity);
                rc.CustomFeesTotal = remainingTotal;
                _db.RoomCharges.Update(rc);
            }
            await _db.SaveChangesAsync(ct);
        }
    }
}