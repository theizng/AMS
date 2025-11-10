using AMS.Models;
using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMS.ViewModels
{
    public partial class ContractEditViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IContractsRepository _repo;
        private readonly IRoomStatusService _roomStatusService; // NEW

        [ObservableProperty] private Contract editable = new Contract();
        [ObservableProperty] private bool isNew;
        [ObservableProperty] private DateTime startDate;
        [ObservableProperty] private DateTime endDate;
        [ObservableProperty] private int dueDay;
        [ObservableProperty] private decimal rentAmount;
        [ObservableProperty] private decimal securityDeposit;
        [ObservableProperty] private int depositReturnDays;
        [ObservableProperty] private int maxOccupants;
        [ObservableProperty] private int maxBikeAllowance;
        [ObservableProperty] private string paymentMethods = "";
        [ObservableProperty] private string lateFeePolicy = "";
        [ObservableProperty] private string propertyDescription = "";
        [ObservableProperty] private string? pdfUrl;
        [ObservableProperty] private bool isBusy;

        public IAsyncRelayCommand SaveCommand { get; }
        public IAsyncRelayCommand ActivateCommand { get; }
        public IAsyncRelayCommand TerminateCommand { get; }

        public ContractEditViewModel(IContractsRepository repo,
                                     IRoomStatusService roomStatusService) // NEW
        {
            _repo = repo;
            _roomStatusService = roomStatusService; // NEW

            SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
            ActivateCommand = new AsyncRelayCommand(ActivateAsync, () => !IsBusy);
            TerminateCommand = new AsyncRelayCommand(TerminateAsync, () => !IsBusy);
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("Contract", out var obj) && obj is Contract c)
            {
                Editable = c;
                StartDate = c.StartDate;
                EndDate = c.EndDate;
                DueDay = c.DueDay;
                RentAmount = c.RentAmount;
                SecurityDeposit = c.SecurityDeposit;
                DepositReturnDays = c.DepositReturnDays;
                MaxOccupants = c.MaxOccupants;
                MaxBikeAllowance = c.MaxBikeAllowance;
                PaymentMethods = c.PaymentMethods;
                LateFeePolicy = c.LateFeePolicy;
                PropertyDescription = c.PropertyDescription;
                PdfUrl = c.PdfUrl;
            }
        }

        public void OnAppearing()
        {
            if (Editable.Status == ContractStatus.Active && Editable.EndDate < DateTime.Today)
                Editable.Status = ContractStatus.Expired;
        }

        private void PushFormIntoEditable()
        {
            Editable.StartDate = StartDate;
            Editable.EndDate = EndDate;
            Editable.DueDay = DueDay;
            Editable.RentAmount = RentAmount;
            Editable.SecurityDeposit = SecurityDeposit;
            Editable.DepositReturnDays = DepositReturnDays;
            Editable.MaxOccupants = MaxOccupants;
            Editable.MaxBikeAllowance = MaxBikeAllowance;
            Editable.PaymentMethods = PaymentMethods?.Trim() ?? "";
            Editable.LateFeePolicy = LateFeePolicy?.Trim() ?? "";
            Editable.PropertyDescription = PropertyDescription?.Trim() ?? "";
            Editable.PdfUrl = PdfUrl?.Trim();
            Editable.UpdatedAt = DateTime.UtcNow;
        }

        private async Task SaveAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                if (EndDate <= StartDate)
                {
                    await Shell.Current.DisplayAlertAsync("Lỗi", "Ngày kết thúc phải sau ngày bắt đầu.", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Editable.ContractId))
                    Editable.ContractId = Guid.NewGuid().ToString("N");

                if (string.IsNullOrWhiteSpace(Editable.ContractNumber))
                    Editable.ContractNumber = "HD-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

                PushFormIntoEditable();

                var existing = await _repo.GetByIdAsync(Editable.ContractId);
                if (existing is null)
                    await _repo.CreateAsync(Editable);
                else
                    await _repo.UpdateAsync(Editable);

                await Shell.Current.DisplayAlertAsync("Thành công", "Đã lưu hợp đồng.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", ex.Message, "OK");
            }
            finally { IsBusy = false; }
        }

        private async Task ActivateAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                if (Editable.Tenants.Count == 0)
                {
                    await Shell.Current.DisplayAlertAsync("Lỗi", "Hợp đồng không có người thuê.", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Editable.PdfUrl))
                {
                    var cont = await Shell.Current.DisplayAlertAsync("Chưa có PDF",
                        "Chưa đính kèm file PDF. Kích hoạt tiếp?", "Kích hoạt", "Hủy");
                    if (!cont) return;
                }

                PushFormIntoEditable();
                Editable.Status = ContractStatus.Active;

                await _repo.UpdateAsync(Editable);
                await _roomStatusService.SetRoomOccupiedAsync(Editable.RoomCode);

                await Shell.Current.DisplayAlertAsync("Thành công", "Hợp đồng đã kích hoạt.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", ex.Message, "OK");
            }
            finally { IsBusy = false; }
        }

        private async Task TerminateAsync()
        {
            if (IsBusy) return;
            var confirm = await Shell.Current.DisplayAlertAsync("Xác nhận", "Chấm dứt hợp đồng này?", "Đồng ý", "Hủy");
            if (!confirm) return;

            IsBusy = true;
            try
            {
                Editable.Status = ContractStatus.Terminated;
                Editable.EndDate = DateTime.Today;
                await _repo.UpdateAsync(Editable);
                await _roomStatusService.SetRoomAvailableAsync(Editable.RoomCode);
                await Shell.Current.DisplayAlertAsync("Thành công", "Hợp đồng đã chấm dứt.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", ex.Message, "OK");
            }
            finally { IsBusy = false; }
        }
    }
}