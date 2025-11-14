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
    public class ContractAddendumService : IContractAddendumService
    {
        private readonly IDbContextFactory<AMSDbContext> _factory;
        private readonly IRoomOccupancyProvider _occupancyProvider;
        private readonly IContractPdfService _pdf;

        public ContractAddendumService(
            IDbContextFactory<AMSDbContext> factory,
            IRoomOccupancyProvider occupancyProvider,
            IContractPdfService pdf)
        {
            _factory = factory;
            _occupancyProvider = occupancyProvider;
            _pdf = pdf;
        }

        public async Task<ContractAddendum> CreateAddendumFromRoomChangeAsync(
            Contract parent, string? reason, DateTime? effectiveDate, LandlordInfo landlord, CancellationToken ct = default)
        {
            var oldSnap = ToSnapshot(parent);
            var occ = await _occupancyProvider.GetTenantsForRoomAsync(parent.RoomCode, ct);
            var newSnap = ToSnapshot(parent);
            newSnap.Tenants = occ.Select(o => new ContractTenant { Name = o.Name, Email = o.Email, Phone = o.Phone }).ToList();

            return await CreateAddendumWithSnapshotsAsync(parent, oldSnap, newSnap, reason, effectiveDate, landlord, ct);
        }

        public async Task<ContractAddendum> CreateAddendumFromCurrentContractAndRoomAsync(
            Contract parent,
            ContractSnapshot uiChangesSnapshot,
            string? reason,
            DateTime? effectiveDate,
            LandlordInfo landlord,
            CancellationToken ct = default)
        {
            // old = current contract as-is (e.g., still A B C)
            var oldSnap = ToSnapshot(parent);

            // new = start from old, overwrite with UI changes, then replace Tenants by current RoomOccupancy
            var newSnap = MergeSnapshots(oldSnap, uiChangesSnapshot);

            var occ = await _occupancyProvider.GetTenantsForRoomAsync(parent.RoomCode, ct);
            newSnap.Tenants = occ.Select(o => new ContractTenant { Name = o.Name, Email = o.Email, Phone = o.Phone }).ToList();

            return await CreateAddendumWithSnapshotsAsync(parent, oldSnap, newSnap, reason, effectiveDate, landlord, ct);
        }

        public async Task<ContractAddendum> CreateAddendumWithSnapshotsAsync(
            Contract parent,
            ContractSnapshot oldSnapshot,
            ContractSnapshot newSnapshot,
            string? reason,
            DateTime? effectiveDate,
            LandlordInfo landlord,
            CancellationToken ct = default)
        {
            await using var db = await _factory.CreateDbContextAsync(ct);

            var parentTracked = await db.Contracts
                .Include(c => c.Addendums)
                .FirstOrDefaultAsync(c => c.ContractId == parent.ContractId, ct)
                ?? throw new InvalidOperationException("Parent contract not found.");

            if (parentTracked.Status != ContractStatus.Active)
                throw new InvalidOperationException("Addendum can only be created for Active contracts.");

            var addendum = new ContractAddendum
            {
                ParentContractId = parentTracked.ContractId,
                AddendumNumber = MakeAddendumNumber(parentTracked),
                Reason = string.IsNullOrWhiteSpace(reason) ? "Điều chỉnh hợp đồng" : reason.Trim(),
                EffectiveDate = effectiveDate ?? DateTime.Today,
                OldTenants = oldSnapshot.Tenants.ToList(),
                NewTenants = newSnapshot.Tenants.ToList(),
                OldSnapshot = oldSnapshot,
                NewSnapshot = newSnapshot
            };

            var pdfPath = await _pdf.GenerateContractAddendumPdfAsync(parentTracked, addendum, landlord, ct);
            addendum.PdfUrl = pdfPath;

            db.ContractAddendums.Add(addendum);

            // Update CONTRACT to the new state (source of truth)
            ApplySnapshotToContract(parentTracked, newSnapshot);
            parentTracked.NeedsAddendum = false;
            parentTracked.AddendumNotifiedAt = null;
            parentTracked.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
            return addendum;
        }

        public async Task<IReadOnlyList<ContractAddendum>> GetAddendumsAsync(string parentContractId, CancellationToken ct = default)
        {
            await using var db = await _factory.CreateDbContextAsync(ct);
            return await db.ContractAddendums
                .AsNoTracking()
                .Where(a => a.ParentContractId == parentContractId)
                .OrderBy(a => a.CreatedAt)
                .ToListAsync(ct);
        }

        private static string MakeAddendumNumber(Contract parent)
        {
            var core = string.IsNullOrWhiteSpace(parent.ContractNumber) ? parent.ContractId : parent.ContractNumber!;
            return $"{core}-PL-{DateTime.UtcNow:yyyyMMddHHmm}";
        }

        private static ContractSnapshot ToSnapshot(Contract c) => new()
        {
            ContractNumber = c.ContractNumber ?? "",
            RoomCode = c.RoomCode,
            HouseAddress = c.HouseAddress,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            DueDay = c.DueDay,
            RentAmount = c.RentAmount,
            SecurityDeposit = c.SecurityDeposit,
            DepositReturnDays = c.DepositReturnDays,
            MaxOccupants = c.MaxOccupants,
            MaxBikeAllowance = c.MaxBikeAllowance,
            PaymentMethods = c.PaymentMethods,
            LateFeePolicy = c.LateFeePolicy,
            PropertyDescription = c.PropertyDescription,
            Tenants = c.Tenants?.ToList() ?? new(),
            PdfUrl = c.PdfUrl
        };

        private static ContractSnapshot MergeSnapshots(ContractSnapshot baseSnap, ContractSnapshot edits)
        {
            // Overwrite fields with UI edits if provided; otherwise keep base
            return new ContractSnapshot
            {
                ContractNumber = string.IsNullOrWhiteSpace(edits.ContractNumber) ? baseSnap.ContractNumber : edits.ContractNumber,
                RoomCode = baseSnap.RoomCode, // RoomCode does not change via UI here
                HouseAddress = string.IsNullOrWhiteSpace(edits.HouseAddress) ? baseSnap.HouseAddress : edits.HouseAddress,
                StartDate = edits.StartDate != default ? edits.StartDate : baseSnap.StartDate,
                EndDate = edits.EndDate != default ? edits.EndDate : baseSnap.EndDate,
                DueDay = edits.DueDay != 0 ? edits.DueDay : baseSnap.DueDay,
                RentAmount = edits.RentAmount != default ? edits.RentAmount : baseSnap.RentAmount,
                SecurityDeposit = edits.SecurityDeposit != default ? edits.SecurityDeposit : baseSnap.SecurityDeposit,
                DepositReturnDays = edits.DepositReturnDays != 0 ? edits.DepositReturnDays : baseSnap.DepositReturnDays,
                MaxOccupants = edits.MaxOccupants != 0 ? edits.MaxOccupants : baseSnap.MaxOccupants,
                MaxBikeAllowance = edits.MaxBikeAllowance != 0 ? edits.MaxBikeAllowance : baseSnap.MaxBikeAllowance,
                PaymentMethods = string.IsNullOrWhiteSpace(edits.PaymentMethods) ? baseSnap.PaymentMethods : edits.PaymentMethods,
                LateFeePolicy = string.IsNullOrWhiteSpace(edits.LateFeePolicy) ? baseSnap.LateFeePolicy : edits.LateFeePolicy,
                PropertyDescription = string.IsNullOrWhiteSpace(edits.PropertyDescription) ? baseSnap.PropertyDescription : edits.PropertyDescription,
                Tenants = baseSnap.Tenants, // tenants will be replaced from occupancy later
                PdfUrl = string.IsNullOrWhiteSpace(edits.PdfUrl) ? baseSnap.PdfUrl : edits.PdfUrl
            };
        }

        private static void ApplySnapshotToContract(Contract target, ContractSnapshot snap)
        {
            target.ContractNumber = string.IsNullOrWhiteSpace(snap.ContractNumber) ? target.ContractNumber : snap.ContractNumber;
            target.RoomCode = snap.RoomCode;
            target.HouseAddress = snap.HouseAddress;

            target.StartDate = snap.StartDate;
            target.EndDate = snap.EndDate;
            target.DueDay = snap.DueDay;

            target.RentAmount = snap.RentAmount;
            target.SecurityDeposit = snap.SecurityDeposit;
            target.DepositReturnDays = snap.DepositReturnDays;

            target.MaxOccupants = snap.MaxOccupants;
            target.MaxBikeAllowance = snap.MaxBikeAllowance;

            target.PaymentMethods = snap.PaymentMethods ?? "";
            target.LateFeePolicy = snap.LateFeePolicy ?? "";
            target.PropertyDescription = snap.PropertyDescription ?? "";

            target.Tenants = snap.Tenants?.ToList() ?? new();
            target.PdfUrl = snap.PdfUrl;
        }
    }
}