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
        private readonly IRoomStatusService _roomStatusService;
        private readonly IRoomOccupancyAdminService _roomOccAdmin;

        [ObservableProperty] private Contract editable = new();
        [ObservableProperty] private bool isBusy;

        // Form fields
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

        // State flags (exposed to UI)
        [ObservableProperty] private bool isPersisted;
        [ObservableProperty] private bool isDraft;
        [ObservableProperty] private bool isActive;
        [ObservableProperty] private bool isTerminated;

        // Action guards for button visibility
        [ObservableProperty] private bool canActivate;
        [ObservableProperty] private bool canTerminate;
        [ObservableProperty] private bool canDelete;
        [ObservableProperty] private bool canGeneratePdf;
        [ObservableProperty] private bool canSendEmail;

        public IAsyncRelayCommand SaveCommand { get; }
        public IAsyncRelayCommand ActivateCommand { get; }
        public IAsyncRelayCommand TerminateCommand { get; }
        public IAsyncRelayCommand GeneratePdfCommand { get; }
        public IAsyncRelayCommand SendContractEmailCommand { get; }

        public ContractEditViewModel(IContractsRepository repo,
                                     IRoomStatusService roomStatusService,
                                     IRoomOccupancyAdminService roomOccAdmin)
        {
            _repo = repo;
            _roomStatusService = roomStatusService;
            _roomOccAdmin = roomOccAdmin;

            SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
            ActivateCommand = new AsyncRelayCommand(ActivateAsync, () => !IsBusy);
            TerminateCommand = new AsyncRelayCommand(TerminateAsync, () => !IsBusy);
            GeneratePdfCommand = new AsyncRelayCommand(GeneratePdfAsync, () => !IsBusy);
            SendContractEmailCommand = new AsyncRelayCommand(SendPdfEmailAsync, () => !IsBusy);
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("Contract", out var obj) && obj is Contract c)
            {
                Editable = c;
                LoadFieldsFromEditable();
                _ = RecalcStateFlagsAsync(); // fire and forget (UI updates when done)
            }
        }

        public void OnAppearing()
        {
            if (Editable.Status == ContractStatus.Active && Editable.EndDate < DateTime.Today)
                Editable.Status = ContractStatus.Expired;
            _ = RecalcStateFlagsAsync();
        }

        private void LoadFieldsFromEditable()
        {
            StartDate = Editable.StartDate;
            EndDate = Editable.EndDate;
            DueDay = Editable.DueDay;
            RentAmount = Editable.RentAmount;
            SecurityDeposit = Editable.SecurityDeposit;
            DepositReturnDays = Editable.DepositReturnDays;
            MaxOccupants = Editable.MaxOccupants;
            MaxBikeAllowance = Editable.MaxBikeAllowance;
            PaymentMethods = Editable.PaymentMethods;
            LateFeePolicy = Editable.LateFeePolicy;
            PropertyDescription = Editable.PropertyDescription;
            PdfUrl = Editable.PdfUrl;
        }

        private void PushFieldsIntoEditable()
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

        private async Task RecalcStateFlagsAsync()
        {
            // Determine persistence by consulting repository only if we have an ID
            bool exists = false;
            if (!string.IsNullOrWhiteSpace(Editable.ContractId))
            {
                var found = await _repo.GetByIdAsync(Editable.ContractId);
                exists = found != null;
            }

            IsPersisted = exists;
            IsDraft = Editable.Status == ContractStatus.Draft;
            IsActive = Editable.Status == ContractStatus.Active;
            IsTerminated = Editable.Status == ContractStatus.Terminated;

            CanActivate = IsPersisted && IsDraft && Editable.Tenants.Count > 0;
            CanTerminate = IsPersisted && IsActive;
            CanDelete = IsPersisted && IsTerminated;      // (Delete not shown in this page currently)
            CanGeneratePdf = IsPersisted && IsDraft;
            CanSendEmail = IsPersisted && !string.IsNullOrWhiteSpace(Editable.PdfUrl);
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

                PushFieldsIntoEditable();

                var existing = await _repo.GetByIdAsync(Editable.ContractId);
                if (existing == null)
                {
                    await _repo.CreateAsync(Editable);
                    IsPersisted = true; // direct flag set so buttons can update faster
                }
                else
                {
                    await _repo.UpdateAsync(Editable);
                }

                await Shell.Current.DisplayAlertAsync("Thành công", "Đã lưu hợp đồng.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
                await RecalcStateFlagsAsync();
            }
        }

        private async Task ActivateAsync()
        {
            if (IsBusy) return;
            if (!CanActivate)
            {
                await Shell.Current.DisplayAlertAsync("Không thể kích hoạt", "Chưa đủ điều kiện kích hoạt.", "OK");
                return;
            }
            IsBusy = true;
            try
            {
                PushFieldsIntoEditable();
                if (Editable.Tenants.Count == 0)
                {
                    await Shell.Current.DisplayAlertAsync("Lỗi", "Hợp đồng không có người thuê.", "OK");
                    return;
                }
                Editable.Status = ContractStatus.Active;
                await _repo.UpdateAsync(Editable);
                await _roomStatusService.SetRoomOccupiedAsync(Editable.RoomCode);
                await Shell.Current.DisplayAlertAsync("Thành công", "Hợp đồng đã kích hoạt.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
                await RecalcStateFlagsAsync();
            }
        }

        private async Task TerminateAsync()
        {
            if (IsBusy) return;
            if (!CanTerminate)
            {
                await Shell.Current.DisplayAlertAsync("Không thể chấm dứt", "Chỉ chấm dứt hợp đồng đang hiệu lực.", "OK");
                return;
            }
            var confirm = await Shell.Current.DisplayAlertAsync("Xác nhận", "Chấm dứt hợp đồng này?", "Đồng ý", "Hủy");
            if (!confirm) return;

            IsBusy = true;
            try
            {
                Editable.Status = ContractStatus.Terminated;
                Editable.EndDate = DateTime.Today;
                await _repo.UpdateAsync(Editable);
                await _roomOccAdmin.EndAllActiveOccupanciesForRoomAsync(Editable.RoomCode);
                await _roomStatusService.SetRoomAvailableAsync(Editable.RoomCode);
                await Shell.Current.DisplayAlertAsync("Thành công", "Đã chấm dứt hợp đồng và giải phóng phòng.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
                await RecalcStateFlagsAsync();
            }
        }

        private async Task GeneratePdfAsync()
        {
            if (!CanGeneratePdf)
            {
                await Shell.Current.DisplayAlertAsync("Không thể tạo PDF", "Chỉ tạo PDF khi hợp đồng ở trạng thái nháp và đã lưu.", "OK");
                return;
            }
            PdfUrl = $"local://contracts/{Editable.ContractId}.pdf";
            PushFieldsIntoEditable();
            await _repo.UpdateAsync(Editable);
            await Shell.Current.DisplayAlertAsync("Thành công", "Đã tạo link PDF mẫu.", "OK");
            await RecalcStateFlagsAsync();
        }

        private async Task SendPdfEmailAsync()
        {
            if (!CanSendEmail)
            {
                await Shell.Current.DisplayAlertAsync("Không thể gửi", "Chưa có PDF hoặc hợp đồng chưa được lưu.", "OK");
                return;
            }
            // TODO: integrate real email sending
            await Shell.Current.DisplayAlertAsync("Thành công", "Đã giả lập gửi email PDF.", "OK");
        }
    }
}