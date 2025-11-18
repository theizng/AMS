using System.Threading;
using System.Threading.Tasks;

namespace AMS.Services.Interfaces
{
    public interface IInvoiceScriptClient
    {
        Task<InvoiceScriptResult> BuildInvoicePdfAsync(InvoiceScriptPayload payload, CancellationToken ct = default);
    }

    public class InvoiceScriptPayload
    {
        // Auth (optional override)
        public string Token { get; set; } = "";

        // Core invoice meta
        public string InvoiceId { get; set; } = "";
        public string RoomCode { get; set; } = "";
        public string InvoiceDateIso { get; set; } = "";        // yyyy-MM-dd
        public string ContractNumber { get; set; } = "";
        public string ContractStartDateIso { get; set; } = "";  // yyyy-MM-dd
        public string PaymentDueDateIso { get; set; } = "";     // yyyy-MM-dd

        // Electric
        public decimal UnitPriceElectric { get; set; }
        public int PreviousElectricReading { get; set; }
        public int CurrentElectricReading { get; set; }
        public string PreviousElectricDateIso { get; set; } = "";
        public string CurrentElectricDateIso { get; set; } = "";

        // Water
        public decimal UnitPriceWater { get; set; }
        public int PreviousWaterReading { get; set; }
        public int CurrentWaterReading { get; set; }
        public string PreviousWaterDateIso { get; set; } = "";
        public string CurrentWaterDateIso { get; set; } = "";

        // Tenants
        public string[] TenantNames { get; set; } = [];
        public string[] TenantPhones { get; set; } = [];
        public string[] TenantEmails { get; set; } = [];

        // Line items (base + custom)
        public decimal BaseRent { get; set; }
        public decimal TotalDue { get; set; }
        public InvoiceLineItem[] CustomLineItems { get; set; } = [];


        // Banking (used for ThongBaoPhi placeholders)
        public string NameAccount { get; set; } = "";  // NEW
        public string BankAccount { get; set; } = "";
        public string BankName { get; set; } = "";     // NEW
        public string Branch { get; set; } = "";       // NEW
    }

    public class InvoiceLineItem
    {
        public string Description { get; set; } = "";
        public string? DescriptionExtra { get; set; }
        public string Unit { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal Amount { get; set; }
    }

    public class InvoiceScriptResult
    {
        public bool Ok { get; set; }
        public string? Error { get; set; }
        public string InvoiceId { get; set; } = "";
        public string PdfName { get; set; } = "";
        public byte[] PdfBytes { get; set; } = [];
        public decimal Total { get; set; }
    }
}