using System.Threading;
using System.Threading.Tasks;

namespace AMS.Services.Interfaces
{
    public interface IInvoiceGenerator
    {
        Task<InvoiceGenerationResult> GenerateAndEmailAsync(string roomCode, CancellationToken ct = default);
    }

    public class InvoiceGenerationResult
    {
        public bool Ok { get; set; }
        public string? Error { get; set; }
        public string? LocalPath { get; set; }
        public string InvoiceId { get; set; } = "";
        public bool EmailsSent { get; set; }
    }
}