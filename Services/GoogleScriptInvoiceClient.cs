using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AMS.Services.Interfaces;

namespace AMS.Services
{
    public class GoogleScriptInvoiceClient : IInvoiceScriptClient
    {
        private readonly HttpClient _http;
        private readonly string _scriptUrl;
        private readonly string _token;

        public GoogleScriptInvoiceClient(HttpClient http, string scriptUrl, string token)
        {
            _http = http;
            _scriptUrl = scriptUrl;
            _token = token;
        }

        public async Task<InvoiceScriptResult> BuildInvoicePdfAsync(InvoiceScriptPayload payload, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_scriptUrl)) throw new InvalidOperationException("Missing script URL.");
            var token = string.IsNullOrWhiteSpace(payload.Token) ? _token : payload.Token;
            if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("Missing script token.");

            var body = new
            {
                token,
                action = "fillInvoiceExportBase64",
                invoiceId = payload.InvoiceId,
                invoiceDateIso = payload.InvoiceDateIso,
                roomCode = payload.RoomCode,
                contractNumber = payload.ContractNumber,
                contractStartDateIso = payload.ContractStartDateIso,
                paymentDueDateIso = payload.PaymentDueDateIso,

                // meters / rates
                unitPriceElectric = payload.UnitPriceElectric,
                previousElectricReading = payload.PreviousElectricReading,
                currentElectricReading = payload.CurrentElectricReading,
                previousElectricDateIso = payload.PreviousElectricDateIso,
                currentElectricDateIso = payload.CurrentElectricDateIso,
                unitPriceWater = payload.UnitPriceWater,
                previousWaterReading = payload.PreviousWaterReading,
                currentWaterReading = payload.CurrentWaterReading,
                previousWaterDateIso = payload.PreviousWaterDateIso,
                currentWaterDateIso = payload.CurrentWaterDateIso,

                // tenants
                tenantNames = payload.TenantNames,
                tenantPhones = payload.TenantPhones,
                tenantEmails = payload.TenantEmails,

                // line items base + custom
                baseRent = payload.BaseRent,
                customLineItems = payload.CustomLineItems,
                thongBaoPhiSum = payload.TotalDue,
                // bank placeholders (NEW)
                nameAccount = payload.NameAccount,
                bankAccount = payload.BankAccount,
                bankName = payload.BankName,
                branch = payload.Branch
            };

            using var resp = await _http.PostAsJsonAsync(_scriptUrl, body, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);
            resp.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (!root.TryGetProperty("ok", out var okEl) || !okEl.GetBoolean())
            {
                var err = root.TryGetProperty("error", out var e) ? e.GetString() : "Unknown script error";
                return new InvoiceScriptResult { Ok = false, Error = err ?? "Script error" };
            }

            var invoiceId = root.GetProperty("invoiceId").GetString() ?? "";
            var pdfName = root.GetProperty("pdfName").GetString() ?? (invoiceId + ".pdf");
            var b64 = root.GetProperty("pdfBase64").GetString() ?? "";
            var total = decimal.Parse(root.GetProperty("total").ToString());

            return new InvoiceScriptResult
            {
                Ok = true,
                InvoiceId = invoiceId,
                PdfName = pdfName,
                PdfBytes = Convert.FromBase64String(b64),
                Total = total
            };
        }
    }
}