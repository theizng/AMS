using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AMS.Models;
using AMS.Services.Interfaces;
using Microsoft.Maui.Storage;

namespace AMS.Services
{
    public class InvoiceGenerator : IInvoiceGenerator
    {
        private readonly IInvoiceScriptClient _scriptClient;
        private readonly IRoomTenantQuery _tenantQuery;
        private readonly IPaymentSettingsProvider _settingsProvider;
        private readonly IEmailNotificationService _emailService;
        private readonly IPaymentsRepository _paymentsRepo;
        private readonly string _token;

        public InvoiceGenerator(IInvoiceScriptClient scriptClient,
                                IRoomTenantQuery tenantQuery,
                                IPaymentSettingsProvider settingsProvider,
                                IEmailNotificationService emailService,
                                IPaymentsRepository paymentsRepo,
                                string token)
        {
            _scriptClient = scriptClient;
            _tenantQuery = tenantQuery;
            _settingsProvider = settingsProvider;
            _emailService = emailService;
            _paymentsRepo = paymentsRepo;
            _token = token;
        }

        public async Task<InvoiceGenerationResult> GenerateAndEmailAsync(string roomCode, CancellationToken ct = default)
        {
            try
            {
                var settings = _settingsProvider.Get();
                var tenantInfo = await _tenantQuery.GetForRoomAsync(roomCode);

                var today = DateTime.Today;
                var invoiceId = $"{today:yyyyMM}-{roomCode}";
                var dueDay = Math.Clamp(settings.DefaultDueDay, 1, 28);
                var paymentDue = new DateTime(today.Year, today.Month, dueDay).ToString("yyyy-MM-dd");

                // TODO: replace with actual readings from your DB
                int prevElec = 0, currElec = 0;
                int prevWater = 0, currWater = 0;

                var payload = new InvoiceScriptPayload
                {
                    Token = _token,
                    InvoiceId = invoiceId,
                    RoomCode = roomCode,
                    InvoiceDateIso = today.ToString("yyyy-MM-dd"),
                    ContractNumber = tenantInfo.ContractNumber ?? "",
                    ContractStartDateIso = tenantInfo.ContractStartDate?.ToString("yyyy-MM-dd") ?? "",
                    PaymentDueDateIso = paymentDue,

                    UnitPriceElectric = settings.DefaultElectricRate,
                    PreviousElectricReading = prevElec,
                    CurrentElectricReading = currElec,

                    UnitPriceWater = settings.DefaultWaterRate,
                    PreviousWaterReading = prevWater,
                    CurrentWaterReading = currWater,

                    TenantNames = tenantInfo.Names.ToArray(),
                    TenantPhones = tenantInfo.Phones.ToArray(),
                    TenantEmails = tenantInfo.Emails.ToArray(),

                    BaseRent = 0m,

                    //Đọc các chi phí có thêm từ CSDL
                    CustomLineItems = Array.Empty<InvoiceLineItem>(),

                    // NEW banking fields
                    NameAccount = settings.NameAccount ?? "",
                    BankAccount = settings.BankAccount ?? "",
                    BankName = settings.BankName ?? "",
                    Branch = settings.Branch ?? ""
                };

                var scriptResult = await _scriptClient.BuildInvoicePdfAsync(payload, ct);
                if (!scriptResult.Ok)
                    return new InvoiceGenerationResult { Ok = false, Error = scriptResult.Error };

                var folder = Path.Combine(FileSystem.AppDataDirectory, "invoices");
                Directory.CreateDirectory(folder);
                var localPath = Path.Combine(folder, scriptResult.PdfName);
                await File.WriteAllBytesAsync(localPath, scriptResult.PdfBytes, ct);

                // Replace manual subject/body + loop with the centralized service
                bool sent = tenantInfo.Emails.Any(e => !string.IsNullOrWhiteSpace(e));
                if (sent)
                {
                    await _emailService.SendInvoicePdfAsync(
                        tenantInfo,
                        roomCode,
                        today.Year,
                        today.Month,
                        settings,
                        localPath,
                        ct);
                }

                return new InvoiceGenerationResult
                {
                    Ok = true,
                    LocalPath = localPath,
                    InvoiceId = invoiceId,
                    EmailsSent = sent
                };
            }
            catch (Exception ex)
            {
                return new InvoiceGenerationResult { Ok = false, Error = ex.Message };
            }
        }
    }
}