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
        private readonly IOnlineMeterSheetWriter? _onlineWriter; // now holds scriptUrl+token internally

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private PaymentCycle? currentCycle;
        [ObservableProperty] private ObservableCollection<MeterRow> meterRows = new();
        [ObservableProperty] private string? sheetUrl;
        [ObservableProperty] private string? localPath;
        [ObservableProperty] private string sourceInfo = "";
        [ObservableProperty] private bool canEditCurrent = true;

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand SyncCommand { get; }
        public IAsyncRelayCommand ConfirmCommand { get; }
        public IAsyncRelayCommand RollForwardCommand { get; }
        public IAsyncRelayCommand PickFileCommand { get; }
        public IAsyncRelayCommand EnterUrlCommand { get; }
        public IAsyncRelayCommand<MeterRow> SaveRowCommand { get; }

        public PaymentMeterEntryViewModel(IPaymentsRepository repo,
                                          IOnlineMeterSheetReader onlineReader,
                                          IMeterSheetReader fileReader,
                                          IOnlineMeterSheetWriter? onlineWriter = null)
        {
            _repo = repo;
            _onlineReader = onlineReader;
            _fileReader = fileReader;
            _onlineWriter = onlineWriter;

            sheetUrl = Preferences.Get("meters:sheet:url", null);
            localPath = Preferences.Get("meters:sheet:path", null);

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            SyncCommand = new AsyncRelayCommand(SyncAsync);
            ConfirmCommand = new AsyncRelayCommand(ConfirmAsync);
            RollForwardCommand = new AsyncRelayCommand(RollForwardAsync);
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
            if (!string.IsNullOrWhiteSpace(SheetUrl)) return "Nguồn: Google Sheet";
            if (!string.IsNullOrWhiteSpace(LocalPath)) return $"Nguồn: File ({Path.GetFileName(LocalPath)})";
            return "Nguồn: (chưa cấu hình)";
        }

        private async Task EnterUrlAsync()
        {
            var input = await Shell.Current.DisplayPromptAsync("URL Sheet", "Dán Google Sheet URL (public share):", "Lưu", "Hủy");
            if (string.IsNullOrWhiteSpace(input)) return;

            if (!GoogleSheetUrlHelper.TryBuildExportXlsxUrl(input, out var normalized))
            {
                await Shell.Current.DisplayAlertAsync("URL không hợp lệ", "Không đúng định dạng Google Sheets.", "OK");
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
                    await Shell.Current.DisplayAlertAsync("Thiếu nguồn", "Chưa cấu hình URL hoặc file.", "OK");
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
                        var m = rows.FirstOrDefault(x => x.RoomCode.Equals(rc.RoomCode, StringComparison.OrdinalIgnoreCase));
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

            if (row.PreviousElectric.HasValue && row.CurrentElectric.HasValue)
                row.ConsumptionElectric = row.CurrentElectric - row.PreviousElectric;
            if (row.PreviousWater.HasValue && row.CurrentWater.HasValue)
                row.ConsumptionWater = row.CurrentWater - row.PreviousWater;

            var charges = await _repo.GetRoomChargesForCycleAsync(CurrentCycle.CycleId);
            var rc = charges.FirstOrDefault(c => c.RoomCode.Equals(row.RoomCode, StringComparison.OrdinalIgnoreCase));
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

            // Optional push to sheet (writer already knows scriptUrl/token)
            if (_onlineWriter != null && !string.IsNullOrWhiteSpace(SheetUrl))
            {
                try
                {
                    await _onlineWriter.UpdateRowAsync(row);
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlertAsync("Sheet update lỗi", ex.Message, "OK");
                }
            }

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
                }
                foreach (var rc in charges)
                    await _repo.UpdateRoomChargeAsync(rc);

                CanEditCurrent = false;
                await Shell.Current.DisplayAlertAsync("Xong", "Đã xác nhận tất cả chỉ số.", "OK");
            }
            finally { IsBusy = false; }
        }

        private async Task RollForwardAsync()
        {
            var confirm = await Shell.Current.DisplayAlertAsync("Chuyển kỳ",
                "Sao chép 'Chỉ số hiện tại' sang 'Chỉ số tháng trước' và xóa 'Chỉ số hiện tại' cho kỳ mới?",
                "Tiếp tục", "Hủy");
            if (!confirm) return;

            if (CurrentCycle == null || IsBusy) return;
            IsBusy = true;
            try
            {
                var charges = await _repo.GetRoomChargesForCycleAsync(CurrentCycle.CycleId);
                foreach (var rc in charges)
                {
                    if (rc.ElectricReading != null)
                    {
                        rc.ElectricReading.Previous = rc.ElectricReading.Current;
                        rc.ElectricReading.Current = 0;
                        rc.ElectricReading.Confirmed = false;
                        rc.ElectricAmount = 0;
                    }
                    if (rc.WaterReading != null)
                    {
                        rc.WaterReading.Previous = rc.WaterReading.Current;
                        rc.WaterReading.Current = 0;
                        rc.WaterReading.Confirmed = false;
                        rc.WaterAmount = 0;
                    }
                    rc.Status = PaymentStatus.MissingData;
                    await _repo.UpdateRoomChargeAsync(rc);
                }

                if (_onlineWriter != null && !string.IsNullOrWhiteSpace(SheetUrl))
                {
                    try
                    {
                        await _onlineWriter.RollForwardAsync();
                    }
                    catch (Exception ex)
                    {
                        await Shell.Current.DisplayAlertAsync("Roll forward sheet lỗi", ex.Message, "OK");
                    }
                }

                CanEditCurrent = true;
                await Shell.Current.DisplayAlertAsync("Hoàn tất", "Đã chuyển kỳ thành công.", "OK");
                await SyncAsync();
            }
            finally { IsBusy = false; }
        }
    }
}