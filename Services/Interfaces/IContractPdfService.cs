using System.Threading;
using System.Threading.Tasks;
using AMS.Models;

namespace AMS.Services.Interfaces
{
    public interface IContractPdfService
    {
        Task<string> GenerateContractPdfAsync(Contract contract, LandlordInfo landlord, CancellationToken ct = default);
        Task<string> GenerateContractAddendumPdfAsync(Contract parent, ContractAddendum addendum, LandlordInfo landlord, CancellationToken ct = default);
    }

    public record LandlordInfo(
        string FullName,
        string Address,
        string IdCardNumber,
        string Phone,
        string? RepresentativeTitle = null);
}