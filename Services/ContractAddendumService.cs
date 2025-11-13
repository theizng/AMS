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
            Contract parent,
            string? reason,
            DateTime? effectiveDate,
            LandlordInfo landlord,
            CancellationToken ct = default)
        {
            await using var db = await _factory.CreateDbContextAsync(ct);

            // Reload parent (and optionally track) to avoid stale state
            var parentTracked = await db.Contracts
                .Include(c => c.Addendums)
                .FirstOrDefaultAsync(c => c.ContractId == parent.ContractId, ct);

            if (parentTracked == null)
                throw new InvalidOperationException("Parent contract not found.");

            if (parentTracked.Status != ContractStatus.Active)
                throw new InvalidOperationException("Addendum can only be created for Active contracts.");

            // Get current room tenants snapshot
            var occ = await _occupancyProvider.GetTenantsForRoomAsync(parentTracked.RoomCode);
            var currentTenants = occ
                .Select(o => new ContractTenant { Name = o.Name, Email = o.Email, Phone = o.Phone })
                .ToList();

            // Prepare addendum: old vs new
            var addendum = new ContractAddendum
            {
                ParentContractId = parentTracked.ContractId,
                AddendumNumber = MakeAddendumNumber(parentTracked),
                Reason = string.IsNullOrWhiteSpace(reason) ? "Điều chỉnh danh sách người thuê" : reason.Trim(),
                EffectiveDate = effectiveDate ?? DateTime.Today,
                OldTenants = new List<ContractTenant>(parentTracked.Tenants),
                NewTenants = currentTenants
            };

            // Generate PDF
            var pdfPath = await _pdf.GenerateContractAddendumPdfAsync(parentTracked, addendum, landlord, ct);
            addendum.PdfUrl = pdfPath;

            // Persist addendum
            db.ContractAddendums.Add(addendum);

            // Update parent snapshot & flags
            parentTracked.Tenants = currentTenants;
            parentTracked.NeedsAddendum = false;
            parentTracked.AddendumNotifiedAt = null;
            parentTracked.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
            return addendum;
        }

        public async Task<IReadOnlyList<ContractAddendum>> GetAddendumsAsync(string parentContractId, CancellationToken ct = default)
        {
            await using var db = await _factory.CreateDbContextAsync(ct);
            var list = await db.ContractAddendums
                .AsNoTracking()
                .Where(a => a.ParentContractId == parentContractId)
                .OrderBy(a => a.CreatedAt)
                .ToListAsync(ct);
            return list;
        }

        private static string MakeAddendumNumber(Contract parent)
        {
            // Simple pattern: {ContractNumber or ID}-PL-{yyyyMMddHHmm}
            var core = string.IsNullOrWhiteSpace(parent.ContractNumber) ? parent.ContractId : parent.ContractNumber!;
            return $"{core}-PL-{DateTime.UtcNow:yyyyMMddHHmm}";
        }
    }
}