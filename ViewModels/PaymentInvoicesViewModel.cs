using AMS.Models;
using AMS.Services;
using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AMS.ViewModels
{
    public partial class PaymentInvoicesViewModel : ObservableObject
    {
        private readonly IPaymentsRepository _repo;
        private readonly IInvoiceScriptClient _script;
        private readonly IRoomTenantQuery _roomQuery;               // still used for titles if needed later
        private readonly IPaymentSettingsProvider _settings;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private int month;
        [ObservableProperty] private int year;
        [ObservableProperty] private PaymentCycle? cycle;
        [ObservableProperty] private ObservableCollection<InvoiceRowVM> items = new();
        [ObservableProperty] private string statusText = "";

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand<InvoiceRowVM> GeneratePdfCommand { get; }

        public PaymentInvoicesViewModel(IPaymentsRepository repo,
                                        IInvoiceScriptClient scriptClient,
                                        IRoomTenantQuery roomQuery,
                                        IPaymentSettingsProvider settings)
        {
            _repo = repo;
            _script = scriptClient;
            _roomQuery = roomQuery;
            _settings = settings;

            var today = DateTime.Today;
            Month = today.Month;
            Year = today.Year;

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            GeneratePdfCommand = new AsyncRelayCommand<InvoiceRowVM>(GeneratePdfAsync);
        }

        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                Cycle = await _repo.GetCycleAsync(Year, Month);
                Items.Clear();
                if (Cycle == null)
                {
                    statusText = "Chưa có chu kỳ. Vào Tổng quan để tạo.";
                    StatusText = statusText;
                    return;
                }
                var charges = await _repo.GetRoomChargesForCycleAsync(Cycle.CycleId);
                foreach (var rc in charges.OrderBy(x => x.RoomCode))
                {
                    var row = new InvoiceRowVM(rc);
                    row.HasPdf = TryFindInvoicePath(Year, Month, rc.RoomCode, out var path);
                    row.LastPdfPath = row.HasPdf ? path : null;
                    Items.Add(row);
                }

                var created = Items.Count(i => i.HasPdf);
                StatusText = $"Đã tạo hóa đơn: {created}/{Items.Count}";
            }
            finally { IsBusy = false; }
        }

        private async Task GeneratePdfAsync(InvoiceRowVM? row)
        {
            if (row == null || Cycle == null) return;

            try
            {
                var s = _settings.Get();
                var info = await _roomQuery.GetForRoomAsync(row.RoomCode);
                var rc = row.Source;

                var payload = BuildPayload(rc, info, s);

                var result = await _script.BuildInvoicePdfAsync(payload);
                if (!result.Ok)
                {
                    row.LastResult = "Lỗi: " + (result.Error ?? "Không rõ");
                    return;
                }

                var path = await SavePdfLocallyAsync(result.PdfName, result.PdfBytes);
                row.LastPdfPath = path;
                row.HasPdf = true;
                row.LastResult = $"Đã tạo/ghi đè PDF: {Path.GetFileName(path)}";
            }
            catch (Exception ex)
            {
                row.LastResult = "Lỗi tạo PDF: " + ex.Message;
            }
        }

        private static async Task<string> SavePdfLocallyAsync(string fileName, byte[] bytes)
        {
            var folder = Path.Combine(FileSystem.AppDataDirectory, "invoices");
            Directory.CreateDirectory(folder);
            var safeName = string.IsNullOrWhiteSpace(fileName) ? $"Invoice_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf" : fileName;
            var path = Path.Combine(folder, safeName);
            await File.WriteAllBytesAsync(path, bytes);
            return path;
        }

        private static bool TryFindInvoicePath(int year, int month, string roomCode, out string path)
        {
            var folder = Path.Combine(FileSystem.AppDataDirectory, "invoices");
            path = string.Empty;
            if (!Directory.Exists(folder)) return false;
            var prefix = $"{year}{month:00}-{roomCode}";
            var file = Directory.GetFiles(folder, "*.pdf").FirstOrDefault(f => Path.GetFileName(f).StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (file == null) return false;
            path = file;
            return true;
        }

        private InvoiceScriptPayload BuildPayload(RoomCharge rc, RoomTenantInfo info, PaymentSettings s)
        {
            var invoiceId = $"{Year}{Month:00}-{rc.RoomCode}";
            var todayIso = DateTime.Today.ToString("yyyy-MM-dd");
            var dueDay = Math.Clamp(s.DefaultDueDay, 1, 28);
            var dueIso = new DateTime(Year, Month, dueDay).ToString("yyyy-MM-dd");

            var items = BuildCustomLineItemsOnly(rc); // Only custom fees. Base lines handled in Apps Script.

            return new InvoiceScriptPayload
            {
                InvoiceId = invoiceId,
                InvoiceDateIso = todayIso,
                RoomCode = rc.RoomCode,

                ContractNumber = info.ContractNumber ?? "",
                ContractStartDateIso = info.ContractStartDate?.ToString("yyyy-MM-dd") ?? "",

                PaymentDueDateIso = dueIso,

                UnitPriceElectric = rc.ElectricReading?.Rate ?? s.DefaultElectricRate,
                PreviousElectricReading = rc.ElectricReading?.Previous ?? 0,
                CurrentElectricReading = rc.ElectricReading?.Current ?? 0,

                UnitPriceWater = rc.WaterReading?.Rate ?? s.DefaultWaterRate,
                PreviousWaterReading = rc.WaterReading?.Previous ?? 0,
                CurrentWaterReading = rc.WaterReading?.Current ?? 0,

                TenantNames = info.Names.ToArray(),
                TenantPhones = info.Phones.ToArray(),
                TenantEmails = info.Emails.ToArray(),

                BaseRent = rc.BaseRent,
                CustomLineItems = items.ToArray(),
                TotalDue = rc.TotalDue,
                NameAccount = s.NameAccount ?? "",
                BankAccount = s.BankAccount ?? "",
                BankName = s.BankName ?? "",
                Branch = s.Branch ?? ""
            };
        }

        // Important: Only custom fees.
        private static System.Collections.Generic.List<InvoiceLineItem> BuildCustomLineItemsOnly(RoomCharge rc)
        {
            var list = new System.Collections.Generic.List<InvoiceLineItem>();

            if (rc.Fees != null && rc.Fees.Count > 0)
            {
                foreach (var f in rc.Fees)
                {
                    list.Add(new InvoiceLineItem
                    {
                        Description = f.Name,
                        Unit = "", // extend FeeInstance if you want to store UnitLabel per instance
                        UnitPrice = f.Rate,
                        Quantity = f.Quantity,
                        Amount = f.Amount
                    });
                }
            }

            return list;
        }
    }

    public partial class InvoiceRowVM : ObservableObject
    {
        public RoomCharge Source { get; }
        public string RoomCode => Source.RoomCode;

        [ObservableProperty] private string summary;
        [ObservableProperty] private string? lastPdfPath;
        [ObservableProperty] private bool hasPdf;
        [ObservableProperty] private string lastResult = "";

        public InvoiceRowVM(RoomCharge rc)
        {
            Source = rc;
            var elecCons = rc.ElectricReading != null ? rc.ElectricReading.Current - rc.ElectricReading.Previous : 0;
            var waterCons = rc.WaterReading != null ? rc.WaterReading.Current - rc.WaterReading.Previous : 0;
            Summary =
                $"Hóa đơn phòng {rc.RoomCode} \nTiền phòng: {rc.BaseRent:N0} đ \n" +
                $"Điện: {elecCons} kWh × {(rc.ElectricReading?.Rate ?? 0):N0} = {rc.ElectricAmount:N0} đ  \n" +
                $"Nước: {waterCons} m³ × {(rc.WaterReading?.Rate ?? 0):N0} = {rc.WaterAmount:N0} đ  \n" +
                $"Phí khác: {rc.CustomFeesTotal:N0} đ | Tổng phí: {(rc.UtilityFeesTotal + rc.CustomFeesTotal):N0} đ\n";
        }
    }
}