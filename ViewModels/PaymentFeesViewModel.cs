using AMS.Models;
using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace AMS.ViewModels
{
    public partial class PaymentFeesViewModel : ObservableObject
    {
        private readonly IPaymentsRepository _repo;
        private readonly IPaymentSettingsProvider _settings;
        private PaymentCycle? _currentCycle;

        [ObservableProperty] private ObservableCollection<FeeType> feeTypes = new();
        [ObservableProperty] private ObservableCollection<RoomCharge> roomCharges = new();
        [ObservableProperty] private decimal defaultElectricRate;
        [ObservableProperty] private decimal defaultWaterRate;
        [ObservableProperty] private bool isBusy;

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand AddFeeTypeCommand { get; }
        public IAsyncRelayCommand SaveFeeTypesCommand { get; }
        public IAsyncRelayCommand<FeeType> DeleteFeeTypeCommand { get; }
        public IAsyncRelayCommand<RoomCharge> AddFeeToRoomCommand { get; }
        public IAsyncRelayCommand DeleteAllFeesCommand { get; }
        public IAsyncRelayCommand SaveDefaultFeesCommand { get; }
        public IAsyncRelayCommand ApplyFeeToAllRoomsCommand { get; }
        public IAsyncRelayCommand<FeeInstance> RemoveFeeInstanceCommand { get; }

        public PaymentFeesViewModel(IPaymentsRepository repo, IPaymentSettingsProvider settings)
        {
            _repo = repo;
            _settings = settings;

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            AddFeeTypeCommand = new AsyncRelayCommand(AddFeeTypeAsync);
            SaveFeeTypesCommand = new AsyncRelayCommand(SaveFeeTypesAsync);
            DeleteFeeTypeCommand = new AsyncRelayCommand<FeeType>(DeleteFeeTypeAsync);
            AddFeeToRoomCommand = new AsyncRelayCommand<RoomCharge>(AddFeeToRoomAsync);
            SaveDefaultFeesCommand = new AsyncRelayCommand(SaveDefaultFeesAsync);
            ApplyFeeToAllRoomsCommand = new AsyncRelayCommand(ApplyFeeToAllRoomsAsync);
            DeleteAllFeesCommand = new AsyncRelayCommand(DeleteAllFeesAsync);
            RemoveFeeInstanceCommand = new AsyncRelayCommand<FeeInstance>(RemoveFeeInstanceAsync);
        }

        private async Task DeleteFeeTypeAsync(FeeType? ft)
        {
            if (ft == null) return;
            var confirm = await Shell.Current.DisplayActionSheet($"Xóa loại phí '{ft.Name}'?", "Hủy", null, "Xóa");
            if (confirm != "Xóa") return;

            FeeTypes.Remove(ft); // removal will persist on SaveFeeTypesCommand (upsert + delete missing)
            await _repo.SaveFeeTypesAsync(FeeTypes);
            await Shell.Current.DisplayAlertAsync("Đã xóa", $"Đã xóa loại phí '{ft.Name}'.", "OK");
        }

        private async Task RemoveFeeInstanceAsync(FeeInstance? fi)
        {
            if (fi == null || _currentCycle == null) return;
            var confirm = await Shell.Current.DisplayActionSheet($"Xóa phí '{fi.Name}' khỏi phòng?", "Hủy", null, "Xóa");
            if (confirm != "Xóa") return;

            await _repo.RemoveFeeFromRoomAsync(fi.RoomChargeId!, fi.FeeInstanceId);

            // Refresh current cycle room charges
            var refreshed = await _repo.GetRoomChargesForCycleAsync(_currentCycle.CycleId);
            RoomCharges = new ObservableCollection<RoomCharge>(refreshed.OrderBy(r => r.RoomCode));
        }

        private async Task DeleteAllFeesAsync()
        {
            if (_currentCycle == null)
            {
                await Shell.Current.DisplayAlertAsync("Chưa sẵn sàng", "Chưa có chu kỳ hiện tại.", "OK");
                return;
            }
            var confirm = await Shell.Current.DisplayActionSheet("Xóa tất cả phí của chu kỳ này?", "Hủy", null, "Xóa");
            if (confirm != "Xóa") return;

            await _repo.ClearFeesForCycleAsync(_currentCycle.CycleId);

            var refreshed = await _repo.GetRoomChargesForCycleAsync(_currentCycle.CycleId);
            RoomCharges = new ObservableCollection<RoomCharge>(refreshed.OrderBy(r => r.RoomCode));

            await Shell.Current.DisplayAlertAsync("Đã xóa", "Đã xóa tất cả phí (tùy chỉnh) của chu kỳ.", "OK");
        }

        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var s = _settings.Get();
                DefaultElectricRate = s.DefaultElectricRate;
                DefaultWaterRate = s.DefaultWaterRate;

                var types = await _repo.GetFeeTypesAsync();
                FeeTypes = new ObservableCollection<FeeType>(types.OrderBy(f => f.Name));
                AttachFeeTypeHandlers();

                var today = DateTime.Today;
                _currentCycle = await _repo.GetCycleAsync(today.Year, today.Month)
                              ?? await _repo.CreateCycleAsync(today.Year, today.Month);

                var charges = await _repo.GetRoomChargesForCycleAsync(_currentCycle.CycleId);
                RoomCharges = new ObservableCollection<RoomCharge>(charges.OrderBy(r => r.RoomCode));
            }
            finally { IsBusy = false; }
        }

        private async Task AddFeeTypeAsync()
        {
            var name = await Shell.Current.DisplayPromptAsync("Tên loại phí", "Nhập tên loại phí:", "OK", "Hủy");
            if (string.IsNullOrWhiteSpace(name)) return;

            var unit = await Shell.Current.DisplayPromptAsync("Đơn vị", "Nhập đơn vị (m³, tháng, lần...) (bỏ trống nếu không):", "OK", "Bỏ qua") ?? "";

            var rateStr = await Shell.Current.DisplayPromptAsync("Đơn giá mặc định", "Nhập đơn giá:", "OK", "Hủy",
                keyboard: Microsoft.Maui.Keyboard.Numeric, initialValue: "0");
            if (string.IsNullOrWhiteSpace(rateStr)) return;
            if (!decimal.TryParse(NormNum(rateStr), out var defaultRate) || defaultRate < 0)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", "Đơn giá không hợp lệ.", "OK");
                return;
            }

            var recurringPick = await Shell.Current.DisplayActionSheet("Phí lặp lại mỗi tháng?", "Hủy", null, "Có", "Không");
            if (string.IsNullOrEmpty(recurringPick) || recurringPick == "Hủy") return;
            bool isRecurring = recurringPick == "Có";

            var activePick = await Shell.Current.DisplayActionSheet("Kích hoạt loại phí?", "Hủy", null, "Có", "Không");
            if (string.IsNullOrEmpty(activePick) || activePick == "Hủy") return;
            bool isActive = activePick == "Có";

            var ft = new FeeType
            {
                Name = name.Trim(),
                UnitLabel = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim(),
                DefaultRate = defaultRate,
                IsRecurring = isRecurring,
                ApplyAllRooms = false,
                Active = isActive
            };

            ft = await _repo.AddFeeTypeAsync(ft);
            FeeTypes.Add(ft);

            await Shell.Current.DisplayAlertAsync("Thành công", "Đã thêm loại phí.", "OK");

            AttachFeeTypeHandlers();
        }

        private async Task SaveFeeTypesAsync()
        {
            await _repo.SaveFeeTypesAsync(FeeTypes);
            await Shell.Current.DisplayAlertAsync("Đã lưu", "Đã lưu danh mục loại phí.", "OK");
        }

        private async Task AddFeeToRoomAsync(RoomCharge? rc)
        {
            if (rc == null) return;

            var active = FeeTypes.Where(f => f.Active).OrderBy(f => f.Name).ToList();
            FeeType? pickedType = null;
            string feeName;

            if (active.Count > 0)
            {
                var names = active.Select(f => f.Name).ToArray();
                var chosen = await Shell.Current.DisplayActionSheet("Chọn loại phí", "Hủy", null, names);
                if (string.IsNullOrEmpty(chosen) || chosen == "Hủy") return;
                pickedType = active.First(x => x.Name == chosen);
                feeName = pickedType.Name;
            }
            else
            {
                var input = await Shell.Current.DisplayPromptAsync("Tên phí", "Nhập tên phí:", "OK", "Hủy");
                if (string.IsNullOrWhiteSpace(input)) return;
                feeName = input.Trim();
            }

            var rateStr = await Shell.Current.DisplayPromptAsync("Đơn giá", "Nhập đơn giá:", "OK", "Hủy",
                keyboard: Microsoft.Maui.Keyboard.Numeric,
                initialValue: pickedType?.DefaultRate.ToString() ?? "0");
            if (string.IsNullOrWhiteSpace(rateStr)) return;
            if (!decimal.TryParse(NormNum(rateStr), out var rate) || rate < 0)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", "Đơn giá không hợp lệ.", "OK");
                return;
            }

            var qtyStr = await Shell.Current.DisplayPromptAsync("Số lượng",
                $"Nhập số lượng{(string.IsNullOrWhiteSpace(pickedType?.UnitLabel) ? "" : $" ({pickedType!.UnitLabel})")}:",
                "OK", "Hủy", keyboard: Microsoft.Maui.Keyboard.Numeric, initialValue: "1");
            if (string.IsNullOrWhiteSpace(qtyStr)) return;
            if (!decimal.TryParse(NormNum(qtyStr), out var qty) || qty <= 0)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", "Số lượng không hợp lệ.", "OK");
                return;
            }

            var fee = new FeeInstance
            {
                FeeTypeId = pickedType?.FeeTypeId,
                Name = feeName,
                Rate = rate,
                Quantity = qty,
                RoomChargeId = rc.RoomChargeId
            };
            await _repo.AddFeeToRoomAsync(rc.RoomChargeId, fee);

            if (_currentCycle != null)
            {
                var refreshed = await _repo.GetRoomChargesForCycleAsync(_currentCycle.CycleId);
                RoomCharges = new ObservableCollection<RoomCharge>(refreshed.OrderBy(r => r.RoomCode));
            }

            await Shell.Current.DisplayAlertAsync("Thành công", $"Đã thêm '{feeName}' cho phòng {rc.RoomCode}.", "OK");
        }

        private async Task ApplyFeeToAllRoomsAsync()
        {
            if (_currentCycle == null || RoomCharges.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Chưa sẵn sàng", "Chưa có chu kỳ/phòng.", "OK");
                return;
            }

            var active = FeeTypes.Where(f => f.Active).OrderBy(f => f.Name).ToList();
            if (active.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Thiếu loại phí", "Hãy thêm/kích hoạt loại phí trước.", "OK");
                return;
            }

            var names = active.Select(f => f.Name).ToArray();
            var chosen = await Shell.Current.DisplayActionSheet("Áp dụng loại phí", "Hủy", null, names);
            if (string.IsNullOrEmpty(chosen) || chosen == "Hủy") return;
            var ft = active.First(x => x.Name == chosen);

            var rateStr = await Shell.Current.DisplayPromptAsync("Đơn giá",
                $"Đơn giá cho '{ft.Name}' (đv: {ft.UnitLabel ?? "-"})",
                "OK", "Hủy", keyboard: Microsoft.Maui.Keyboard.Numeric,
                initialValue: ft.DefaultRate.ToString());
            if (string.IsNullOrWhiteSpace(rateStr)) return;
            if (!decimal.TryParse(NormNum(rateStr), out var rate) || rate < 0)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", "Đơn giá không hợp lệ.", "OK");
                return;
            }

            var qtyStr = await Shell.Current.DisplayPromptAsync("Số lượng", "Nhập số lượng:", "OK", "Hủy",
                keyboard: Microsoft.Maui.Keyboard.Numeric, initialValue: "1");
            if (string.IsNullOrWhiteSpace(qtyStr)) return;
            if (!decimal.TryParse(NormNum(qtyStr), out var qty) || qty <= 0)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", "Số lượng không hợp lệ.", "OK");
                return;
            }

            var template = new FeeInstance
            {
                FeeTypeId = ft.FeeTypeId,
                Name = ft.Name,
                Rate = rate,
                Quantity = qty
            };
            await _repo.ApplyFeeToAllRoomsAsync(_currentCycle.CycleId, template);

            var refreshed = await _repo.GetRoomChargesForCycleAsync(_currentCycle.CycleId);
            RoomCharges = new ObservableCollection<RoomCharge>(refreshed.OrderBy(r => r.RoomCode));

            await Shell.Current.DisplayAlertAsync("Thành công", $"Đã áp dụng '{ft.Name}' cho {RoomCharges.Count} phòng.", "OK");
        }

        private async Task SaveDefaultFeesAsync()
        {
            var s = _settings.Get();
            s.DefaultElectricRate = DefaultElectricRate;
            s.DefaultWaterRate = DefaultWaterRate;
            await _settings.SaveAsync(s);

            if (_currentCycle != null)
            {
                var list = await _repo.GetRoomChargesForCycleAsync(_currentCycle.CycleId);
                foreach (var rc in list)
                {
                    rc.ElectricReading ??= new ElectricReading();
                    rc.WaterReading ??= new WaterReading();

                    // Only update rates; do NOT recalculate amounts here
                    rc.ElectricReading.Rate = DefaultElectricRate;
                    rc.WaterReading.Rate = DefaultWaterRate;

                    // Amounts will be recomputed later by explicit meter sync/save action
                    await _repo.UpdateRoomChargeAsync(rc);
                }
                RoomCharges = new ObservableCollection<RoomCharge>(list.OrderBy(r => r.RoomCode));
            }

            await Shell.Current.DisplayAlertAsync("Đã lưu", "Đã lưu phí mặc định (chỉ cập nhật đơn giá).\nVui lòng đọc lại chỉ số điện/nước để cập nhật đúng giá tiền\nTài chính -> Ghi số điện / nước", "OK");
        }

        private void AttachFeeTypeHandlers()
        {
            foreach (var ft in FeeTypes)
            {
                ft.PropertyChanged -= FeeType_PropertyChanged;
                ft.PropertyChanged += FeeType_PropertyChanged;
            }
        }

        private async void FeeType_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not FeeType ft) return;
            if (e.PropertyName == nameof(FeeType.IsRecurring))
            {
                if (ft.IsRecurring)
                {
                    var ok = await Shell.Current.DisplayAlert("Bật định kỳ",
                        $"Phí '{ft.Name}' sẽ được áp dụng vào TẤT CẢ chu kỳ hiện có (mỗi phòng 1 dòng).\n" +
                        "Thao tác này không tự động áp dụng cho chu kỳ tạo sau này – nếu tạo chu kỳ mới hãy tắt rồi bật lại.\nTiếp tục?",
                        "Áp dụng", "Hủy");
                    if (!ok)
                    {
                        ft.IsRecurring = false;
                        return;
                    }
                    await _repo.ApplyFeeTypeToAllExistingCyclesAsync(ft);
                    await Shell.Current.DisplayAlertAsync("Đã áp dụng",
                        "Đã thêm phí vào tất cả chu kỳ hiện có. Chu kỳ mới tạo sau sẽ cần thao tác lại.",
                        "OK");
                }
                else
                {
                    var ok = await Shell.Current.DisplayAlert("Tắt định kỳ",
                        $"Xóa tất cả dòng phí '{ft.Name}' khỏi mọi chu kỳ hiện có?\nTiếp tục?",
                        "Xóa", "Hủy");
                    if (!ok)
                    {
                        ft.IsRecurring = true;
                        return;
                    }
                    await _repo.RemoveFeeTypeFromAllCyclesAsync(ft.FeeTypeId);
                    await Shell.Current.DisplayAlertAsync("Đã xóa", "Đã gỡ phí khỏi tất cả chu kỳ.", "OK");
                }
            }
            else if (e.PropertyName == nameof(FeeType.ApplyAllRooms))
            {
                if (_currentCycle == null) return;

                if (ft.ApplyAllRooms)
                {
                    var ok = await Shell.Current.DisplayAlert("Áp dụng tất cả phòng",
                        $"Áp dụng phí '{ft.Name}' cho TẤT CẢ phòng của chu kỳ hiện tại?\nTiếp tục?",
                        "Áp dụng", "Hủy");
                    if (!ok)
                    {
                        ft.ApplyAllRooms = false;
                        return;
                    }

                    var charges = await _repo.GetRoomChargesForCycleAsync(_currentCycle.CycleId);
                    foreach (var rc in charges)
                    {
                        if (rc.Fees.Any(f => f.FeeTypeId == ft.FeeTypeId)) continue;
                        var fi = new FeeInstance
                        {
                            RoomChargeId = rc.RoomChargeId,
                            FeeTypeId = ft.FeeTypeId,
                            Name = ft.Name,
                            Rate = ft.DefaultRate,
                            Quantity = 1
                        };
                        await _repo.AddFeeToRoomAsync(rc.RoomChargeId, fi);
                    }
                    var refreshed = await _repo.GetRoomChargesForCycleAsync(_currentCycle.CycleId);
                    RoomCharges = new ObservableCollection<RoomCharge>(refreshed.OrderBy(r => r.RoomCode));
                    await Shell.Current.DisplayAlertAsync("Đã áp dụng", $"Đã áp dụng '{ft.Name}' cho tất cả phòng.", "OK");
                }
                else
                {
                    var ok = await Shell.Current.DisplayAlert("Gỡ phí khỏi tất cả phòng",
                        $"Gỡ phí '{ft.Name}' khỏi chu kỳ hiện tại?\nTiếp tục?",
                        "Gỡ", "Hủy");
                    if (!ok)
                    {
                        ft.ApplyAllRooms = true;
                        return;
                    }
                    await _repo.RemoveFeeTypeFromCycleAsync(ft.FeeTypeId, _currentCycle.CycleId);
                    var refreshed = await _repo.GetRoomChargesForCycleAsync(_currentCycle.CycleId);
                    RoomCharges = new ObservableCollection<RoomCharge>(refreshed.OrderBy(r => r.RoomCode));
                    await Shell.Current.DisplayAlertAsync("Đã gỡ", $"Đã gỡ '{ft.Name}' khỏi tất cả phòng (chu kỳ hiện tại).", "OK");
                }
            }
        }

        private static string NormNum(string s) => s.Replace(".", "").Replace(",", "").Trim();
    }
}