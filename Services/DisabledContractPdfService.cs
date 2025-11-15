using System;
using System.Threading;
using System.Threading.Tasks;
using AMS.Models;
using AMS.Services.Interfaces;

namespace AMS.Services
{
    /// <summary>
    /// PDF service stub for platforms without QuestPDF support (Android / iOS).
    /// Generates no file and returns a user-friendly exception for callers who ignore CanGeneratePdf flag.
    /// </summary>
#if !WINDOWS && !MACCATALYST && !LINUX
    public class DisabledContractPdfService : IContractPdfService
    {
        private Task<string> Fail(string feature) =>
            Task.FromException<string>(new PlatformNotSupportedException(
                $"{feature} không khả dụng trên nền tảng này. (QuestPDF chỉ hỗ trợ Desktop)"));

        public Task<string> GenerateContractPdfAsync(Contract contract, LandlordInfo landlord, CancellationToken ct = default)
            => Fail("Tạo PDF hợp đồng");

        public Task<string> GenerateContractAddendumPdfAsync(Contract parent, ContractAddendum addendum, LandlordInfo landlord, CancellationToken ct = default)
            => Fail("Tạo PDF phụ lục");
    }
#endif
}