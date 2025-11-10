using System.Threading.Tasks;
using AMS.Models;

namespace AMS.Services.Interfaces
{
    public interface IContractPdfService
    {
        Task<string> GeneratePdfAsync(Contract contract); // returns file path or URL
    }
}