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
    public partial class PaymentMeterEntryViewModel : ObservableObject
    {
        private readonly IPaymentsRepository _repo;
        private readonly IOnlineMeterSheetReader _onlineReader;
        private readonly IMeterSheetReader _fileReader;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private PaymentCycle? currentCycle;
        [ObservableProperty] private ObservableCollection<MeterRow> meterRows = new();
        [ObservableProperty] private string? sheetUrl;
        [ObservableProperty] private string? localPath;
        [ObservableProperty] private string sourceInfo = "";

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand SyncCommand { get; }
        public IAsyncRelayCommand ConfirmCommand { get; }
        public IAsyncRelayCommand PickFileCommand { get; }
        public IAsyncRelayCommand EnterUrlCommand { get; }
        public IAsyncRelayCommand<MeterRow> SaveRowCommand { get; }

        public PaymentMeterEntryViewModel(IPaymentsRepository repo,
                                   IOnlineMeterSheetReader onlineReader,
                                   IMeterSheetReader fileReader)
        {
            _repo = repo;
            _onlineReader = onlineReader;
            _fileReader = fileReader;

            sheetUrl = Preferences.Get("meters:sheet:url", null);
            localPath = Preferences.Get("meters:sheet:path", null);

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            SyncCommand = new AsyncRelayCommand(SyncAsync);
            ConfirmCommand = new AsyncRelayCommand(ConfirmAsync);
            PickFileCommand = new AsyncRelayCommand(PickFileAsync);
            EnterUrlCommand = new AsyncRelayCommand(EnterUrlAsync);
            SaveRowCommand = new AsyncRelayCommand<MeterRow>(SaveRowAsync);
        }

        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var today = DateTime.Today;
                CurrentCycle = await _repo.GetCycleAsync(today.Year, today.Month)
                               ?? await _repo.CreateCycleAsync(today.Year, today.Month);

                SourceInfo = BuildSourceInfo();
            }
            finally { IsBusy = false; }
        }

        private string BuildSourceInfo()
        {
            if (!string.IsNullOrWhiteSpace(SheetUrl)) return "Nguồn: Google Sheet (URL)";
            if (!string.IsNullOrWhiteSpace(LocalPath)) return $"Nguồn: File cục bộ ({Path.GetFileName(LocalPath)})";
            return "Nguồn: (chưa cấu hình)";
        }

        private async Task EnterUrlAsync()
        {
            var input = await Shell.Current.DisplayPromptAsync("URL Sheet", "Dán Google Sheet URL:", "Lưu", "Hủy");
            if (string.IsNullOrWhiteSpace(input)) return;

            if (!GoogleSheetUrlHelper.TryBuildExportXlsxUrl(input, out var normalized))
            {
                await Shell.Current.DisplayAlertAsync("URL không hợp lệ", "Không phải đường dẫn Google Sheets.", "OK");
                return;
            }
            SheetUrl = normalized;
            LocalPath = null;
            Preferences.Set("meters:sheet:url", SheetUrl);
            Preferences.Remove("meters:sheet:path");
            SourceInfo = BuildSourceInfo();
        }

        private async Task PickFileAsync()
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Chọn file chỉ số (.xlsx)"
            });
            if (result == null) return;
            LocalPath = result.FullPath;
            SheetUrl = null;
            Preferences.Set("meters:sheet:path", LocalPath);
            Preferences.Remove("meters:sheet:url");
            SourceInfo = BuildSourceInfo();
        }

        private async Task SyncAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                IReadOnlyList<MeterRow> rows;
                if (!string.IsNullOrWhiteSpace(SheetUrl))
                {
                    var urlWithCb = SheetUrl.Contains("?") ? $"{SheetUrl}&cb={DateTime.UtcNow.Ticks}" : $"{SheetUrl}?cb={DateTime.UtcNow.Ticks}";
                    rows = await _onlineReader.ReadFromUrlAsync(urlWithCb);
                }
                else if (!string.IsNullOrWhiteSpace(LocalPath))
                {
                    rows = await _fileReader.ReadAsync(LocalPath);
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync("Thiếu nguồn", "Chưa có URL hoặc file.", "OK");
                    return;
                }

                foreach (var r in rows)
                {
                    if (!r.ConsumptionElectric.HasValue && r.PreviousElectric.HasValue && r.CurrentElectric.HasValue)
                        r.ConsumptionElectric = r.CurrentElectric - r.PreviousElectric;
                    if (!r.ConsumptionWater.HasValue && r.PreviousWater.HasValue && r.CurrentWater.HasValue)
                        r.ConsumptionWater = r.CurrentWater - r.PreviousWater;
                }

                MeterRows = new ObservableCollection<MeterRow>(rows.OrderBy(r => r.RoomCode));

                if (CurrentCycle != null)
                {
                    var charges = await _repo.GetRoomChargesForCycleAsync(CurrentCycle.CycleId);
                    foreach (var rc in charges)
                    {
                        var m = rows.FirstOrDefault(x => string.Equals(x.RoomCode, rc.RoomCode, StringComparison.OrdinalIgnoreCase));
                        if (m == null) continue;

                        rc.ElectricReading ??= new ElectricReading();
                        if (m.PreviousElectric.HasValue) rc.ElectricReading.Previous = m.PreviousElectric.Value;
                        if (m.CurrentElectric.HasValue) rc.ElectricReading.Current = m.CurrentElectric.Value;
                        rc.ElectricAmount = rc.ElectricReading.Amount;

                        rc.WaterReading ??= new WaterReading();
                        if (m.PreviousWater.HasValue) rc.WaterReading.Previous = m.PreviousWater.Value;
                        if (m.CurrentWater.HasValue) rc.WaterReading.Current = m.CurrentWater.Value;
                        rc.WaterAmount = rc.WaterReading.Amount;

                        if (rc.Status == PaymentStatus.MissingData &&
                            (m.CurrentElectric.HasValue || m.CurrentWater.HasValue))
                            rc.Status = PaymentStatus.ReadyToSend;

                        await _repo.UpdateRoomChargeAsync(rc);
                    }
                }

                await Shell.Current.DisplayAlertAsync("Đã đồng bộ", $"Đọc {MeterRows.Count} dòng.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", ex.Message, "OK");
            }
            finally { IsBusy = false; }
        }

        private async Task SaveRowAsync(MeterRow? row)
        {
            if (row == null || CurrentCycle == null) return;

            if (!row.ConsumptionElectric.HasValue && row.PreviousElectric.HasValue && row.CurrentElectric.HasValue)
                row.ConsumptionElectric = row.CurrentElectric - row.PreviousElectric;
            if (!row.ConsumptionWater.HasValue && row.PreviousWater.HasValue && row.CurrentWater.HasValue)
                row.ConsumptionWater = row.CurrentWater - row.PreviousWater;

            var charges = await _repo.GetRoomChargesForCycleAsync(CurrentCycle.CycleId);
            var rc = charges.FirstOrDefault(c => string.Equals(c.RoomCode, row.RoomCode, StringComparison.OrdinalIgnoreCase));
            if (rc == null) return;

            rc.ElectricReading ??= new ElectricReading();
            if (row.PreviousElectric.HasValue) rc.ElectricReading.Previous = row.PreviousElectric.Value;
            if (row.CurrentElectric.HasValue) rc.ElectricReading.Current = row.CurrentElectric.Value;

            rc.WaterReading ??= new WaterReading();
            if (row.PreviousWater.HasValue) rc.WaterReading.Previous = row.PreviousWater.Value;
            if (row.CurrentWater.HasValue) rc.WaterReading.Current = row.CurrentWater.Value;

            rc.ElectricAmount = rc.ElectricReading.Amount;
            rc.WaterAmount = rc.WaterReading.Amount;

            if (rc.Status == PaymentStatus.MissingData &&
               (row.CurrentElectric.HasValue || row.CurrentWater.HasValue))
                rc.Status = PaymentStatus.ReadyToSend;

            await _repo.UpdateRoomChargeAsync(rc);
            await Shell.Current.DisplayAlertAsync("Đã lưu", $"Đã cập nhật phòng {row.RoomCode}.", "OK");
        }

        private async Task ConfirmAsync()
        {
            if (CurrentCycle == null || IsBusy) return;
            IsBusy = true;
            try
            {
                var charges = await _repo.GetRoomChargesForCycleAsync(CurrentCycle.CycleId);
                foreach (var rc in charges)
                {
                    if (rc.ElectricReading != null)
                    {
                        rc.ElectricReading.Confirmed = true;
                        rc.ElectricAmount = rc.ElectricReading.Amount;
                    }
                    if (rc.WaterReading != null)
                    {
                        rc.WaterReading.Confirmed = true;
                        rc.WaterAmount = rc.WaterReading.Amount;
                    }
                    await _repo.UpdateRoomChargeAsync(rc);
                }
                await Shell.Current.DisplayAlertAsync("Xong", "Đã xác nhận tất cả chỉ số.", "OK");
            }
            finally { IsBusy = false; }
        }
    }
}