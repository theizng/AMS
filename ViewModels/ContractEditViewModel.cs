using AMS.Models;
using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AMS.ViewModels
{
    public partial class ContractEditViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IPdfCapabilityService _pdfCapability;
        private readonly IContractsRepository _repo;
        private readonly IRoomStatusService _roomStatusService;
        private readonly IRoomOccupancyAdminService _roomOccAdmin;
        private readonly IEmailNotificationService _emailNotify;
        private readonly IContractPdfService _pdfService;
        private readonly IAuthService _auth;

        private readonly IContractAddendumService _addendumService;
        private readonly IRoomOccupancyProvider _occupancyProvider;

        [ObservableProperty] private Contract editable = new();
        [ObservableProperty] private bool isBusy;

        [ObservableProperty] private bool isReadOnly;
        [ObservableProperty] private bool canCreateAddendum;

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

        [ObservableProperty] private bool isPersisted;
        [ObservableProperty] private bool isDraft;
        [ObservableProperty] private bool isActive;
        [ObservableProperty] private bool isTerminated;

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
        public IAsyncRelayCommand CreateAddendumCommand { get; }
        public IAsyncRelayCommand ViewHistoryCommand { get; }

        private string? _originalEditHash;
        private ContractSnapshot? _loadedSnapshot;

        public ContractEditViewModel(IContractsRepository repo,
                                     IRoomStatusService roomStatusService,
                                     IRoomOccupancyAdminService roomOccAdmin,
                                     IEmailNotificationService emailNotify,
                                     IContractPdfService contractPdfService,
                                     IContractAddendumService addendumService,
                                     IRoomOccupancyProvider occupancyProvider,
                                     IPdfCapabilityService pdfCapability,
                                     IAuthService auth)
        {
            _repo = repo;
            _roomStatusService = roomStatusService;
            _roomOccAdmin = roomOccAdmin;
            _emailNotify = emailNotify;
            _pdfService = contractPdfService;
            _addendumService = addendumService;
            _occupancyProvider = occupancyProvider;
            _pdfCapability = pdfCapability;
            _auth = auth;

            SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
            ActivateCommand = new AsyncRelayCommand(ActivateAsync, () => !IsBusy);
            TerminateCommand = new AsyncRelayCommand(TerminateAsync, () => !IsBusy);
            GeneratePdfCommand = new AsyncRelayCommand(GeneratePdfAsync, () => !IsBusy);
            SendContractEmailCommand = new AsyncRelayCommand(SendPdfEmailAsync, () => !IsBusy);
            CreateAddendumCommand = new AsyncRelayCommand(CreateAddendumAsync, () => !IsBusy);
            ViewHistoryCommand = new AsyncRelayCommand(ViewHistoryAsync, () => !IsBusy);
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            IsReadOnly = false;

            if (query.TryGetValue("Contract", out var obj) && obj is Contract c)
                Editable = c;

            if (query.TryGetValue("readonly", out var ro) && ro is bool b)
                IsReadOnly = b;
            else if (query.TryGetValue("readonly", out var ros) && bool.TryParse(ros?.ToString(), out var b2))
                IsReadOnly = b2;

            LoadFieldsFromEditable();
            _loadedSnapshot = BuildSnapshotFrom(Editable);
            _originalEditHash = ComputeEditHash(Editable);
            _ = RecalcStateFlagsAsync();
        }

        public void OnAppearing()
        {
            if (!IsReadOnly && Editable.Status == ContractStatus.Active && Editable.EndDate < DateTime.Today)
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

            CanCreateAddendum = IsPersisted && IsActive && Editable.NeedsAddendum;
            CanActivate = IsPersisted && IsDraft && Editable.Tenants.Count > 0;
            CanTerminate = false;
            CanDelete = IsPersisted && IsTerminated;

            // Show "Tạo PDF" when no PDF exists (and feature is supported, not readonly)
            CanGeneratePdf = _pdfCapability.IsSupported && IsPersisted && !IsReadOnly && string.IsNullOrWhiteSpace(Editable.PdfUrl);
            // Show "Gửi PDF" only when a PDF exists
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

                // Apply UI changes
                PushFieldsIntoEditable();

                var newHash = ComputeEditHash(Editable);
                var changedWhileActive = IsActive && _originalEditHash != null && _originalEditHash != newHash;

                if (changedWhileActive)
                {
                    var proceed = await Shell.Current.DisplayAlertAsync(
                        "Hợp đồng đang hiệu lực",
                        "Bạn đang sửa hợp đồng đang hiệu lực. Áp dụng thay đổi sẽ tạo PHỤ LỤC hợp đồng. Tiếp tục?",
                        "Tạo phụ lục",
                        "Hủy");

                    if (!proceed) return;

                    var landlord = BuildLandlordInfo();

                    var oldSnapshot = _loadedSnapshot ?? BuildSnapshotFrom(Editable);
                    var newSnapshot = BuildSnapshotFrom(Editable);

                    var add = await _addendumService.CreateAddendumWithSnapshotsAsync(
                        Editable, oldSnapshot, newSnapshot, "Điều chỉnh điều khoản hợp đồng", DateTime.Today, landlord);

                    // Reload parent updated by service
                    var reloaded = await _repo.GetByIdAsync(Editable.ContractId);
                    if (reloaded != null)
                    {
                        Editable = reloaded;

                        // Force newest PDF regeneration after an effective update
                        Editable.PdfUrl = null;
                        PdfUrl = null;
                        await _repo.UpdateAsync(Editable);

                        LoadFieldsFromEditable();
                        _loadedSnapshot = BuildSnapshotFrom(Editable);
                        _originalEditHash = ComputeEditHash(Editable);
                    }

                    await Shell.Current.DisplayAlertAsync("Thành công", $"Đã tạo phụ lục {add.AddendumNumber} và áp dụng thay đổi.", "OK");
                    await RecalcStateFlagsAsync();
                    return;
                }

                // Normal save (Draft or Active with no tracked change)
                var existing = await _repo.GetByIdAsync(Editable.ContractId);
                if (existing == null)
                {
                    await _repo.CreateAsync(Editable);
                    IsPersisted = true;
                }
                else
                {
                    // If fields changed, clear PdfUrl to require re-generation
                    if (_originalEditHash != null && _originalEditHash != newHash)
                    {
                        Editable.PdfUrl = null;
                        PdfUrl = null;
                    }
                    await _repo.UpdateAsync(Editable);
                }

                _loadedSnapshot = BuildSnapshotFrom(Editable);
                _originalEditHash = newHash;

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
                await _emailNotify.SendContractActivatedAsync(Editable);
                _loadedSnapshot = BuildSnapshotFrom(Editable);
                _originalEditHash = ComputeEditHash(Editable);
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
                await _emailNotify.SendContractTerminatedAsync(Editable);
                _loadedSnapshot = BuildSnapshotFrom(Editable);
                _originalEditHash = ComputeEditHash(Editable);
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
            if (!_pdfCapability.IsSupported)
            {
                await Shell.Current.DisplayAlertAsync("Không hỗ trợ", "Tính năng tạo PDF chỉ khả dụng trên phiên bản Desktop.", "OK");
                return;
            }
            if (!CanGeneratePdf)
            {
                await Shell.Current.DisplayAlertAsync("Không thể tạo PDF", "Chức năng này không khả dụng ở chế độ hiện tại.", "OK");
                return;
            }

            var landlord = BuildLandlordInfo();
            var path = await _pdfService.GenerateContractPdfAsync(Editable, landlord);
            PdfUrl = path;
            PushFieldsIntoEditable();
            await _repo.UpdateAsync(Editable);
            _loadedSnapshot = BuildSnapshotFrom(Editable);
            _originalEditHash = ComputeEditHash(Editable);
            await Shell.Current.DisplayAlertAsync("Thành công", "Đã tạo hợp đồng điện tử PDF và lưu đường dẫn.", "OK");
            await RecalcStateFlagsAsync();
        }

        private async Task SendPdfEmailAsync()
        {
            if (!CanSendEmail)
            {
                await Shell.Current.DisplayAlertAsync("Không thể gửi", "Chưa có PDF hoặc hợp đồng chưa được lưu.", "OK");
                return;
            }
            await _emailNotify.SendContractPdfAsync(Editable);
            await Shell.Current.DisplayAlertAsync("Thành công", "Đã gửi hợp đồng điện tử PDF.", "OK");
        }

        private async Task CreateAddendumAsync()
        {
            if (!CanCreateAddendum)
            {
                await Shell.Current.DisplayAlertAsync("Phụ lục", "Chỉ tạo phụ lục khi hợp đồng đang hiệu lực và cần phụ lục.", "OK");
                return;
            }

            try
            {
                var latest = await _repo.GetByIdAsync(Editable.ContractId) ?? Editable;

                var uiSnap = BuildUiChangesSnapshot();

                var occDtos = await _occupancyProvider.GetTenantsForRoomAsync(latest.RoomCode);
                var newTenantNames = string.Join(", ", occDtos.Select(o => o.Name));
                var oldTenantNames = string.Join(", ", latest.Tenants.Select(t => t.Name));

                var summary = $"Trước: {oldTenantNames}\nSau: {newTenantNames}";
                var proceed = await Shell.Current.DisplayAlertAsync("Xác nhận tạo phụ lục", summary, "Tạo", "Hủy");
                if (!proceed) return;

                var reason = await Shell.Current.DisplayPromptAsync("Phụ lục", "Lý do thay đổi (tùy chọn):", "OK", "Bỏ qua", maxLength: 200);
                if (reason == "Bỏ qua") reason = null;

                var landlord = BuildLandlordInfo();

                var add = await _addendumService.CreateAddendumFromCurrentContractAndRoomAsync(
                    latest, uiSnap, reason, DateTime.Today, landlord);

                var reloaded = await _repo.GetByIdAsync(Editable.ContractId);
                if (reloaded != null)
                {
                    Editable = reloaded;
                    // Force newest PDF regeneration after update
                    Editable.PdfUrl = null;
                    PdfUrl = null;
                    await _repo.UpdateAsync(Editable);

                    LoadFieldsFromEditable();
                    _originalEditHash = ComputeEditHash(Editable);
                }

                await Shell.Current.DisplayAlertAsync("Thành công", $"Đã tạo phụ lục {add.AddendumNumber}.", "OK");
                await RecalcStateFlagsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", ex.Message, "OK");
            }
        }

        private async Task ViewHistoryAsync()
        {
            // Remove history access from edit page; use list page only
            await Shell.Current.DisplayAlertAsync("Lịch sử", "Vui lòng xem lịch sử từ danh sách hợp đồng.", "OK");
        }

        private ContractSnapshot BuildUiChangesSnapshot()
        {
            return new ContractSnapshot
            {
                ContractNumber = Editable.ContractNumber ?? "",
                RoomCode = Editable.RoomCode,
                HouseAddress = Editable.HouseAddress,
                StartDate = StartDate,
                EndDate = EndDate,
                DueDay = DueDay,
                RentAmount = RentAmount,
                SecurityDeposit = SecurityDeposit,
                DepositReturnDays = DepositReturnDays,
                MaxOccupants = MaxOccupants,
                MaxBikeAllowance = MaxBikeAllowance,
                PaymentMethods = PaymentMethods,
                LateFeePolicy = LateFeePolicy,
                PropertyDescription = PropertyDescription,
                Tenants = Editable.Tenants.ToList(),
                PdfUrl = PdfUrl
            };
        }

        private ContractSnapshot BuildSnapshotFrom(Contract c)
        {
            return new ContractSnapshot
            {
                ContractNumber = c.ContractNumber ?? "",
                RoomCode = c.RoomCode,
                HouseAddress = c.HouseAddress,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                DueDay = c.DueDay,
                RentAmount = c.RentAmount,
                SecurityDeposit = c.SecurityDeposit,
                DepositReturnDays = c.DepositReturnDays,
                MaxOccupants = c.MaxOccupants,
                MaxBikeAllowance = c.MaxBikeAllowance,
                PaymentMethods = c.PaymentMethods,
                LateFeePolicy = c.LateFeePolicy,
                PropertyDescription = c.PropertyDescription,
                Tenants = c.Tenants?.ToList() ?? new(),
                PdfUrl = c.PdfUrl
            };
        }

        private static Contract MakePreviewFrom(ContractSnapshot snap, string? labelSuffix)
        {
            return new Contract
            {
                ContractId = Guid.NewGuid().ToString("N"),
                ContractNumber = string.IsNullOrWhiteSpace(labelSuffix) ? snap.ContractNumber : $"{snap.ContractNumber}{labelSuffix}",
                RoomCode = snap.RoomCode,
                HouseAddress = snap.HouseAddress,
                StartDate = snap.StartDate,
                EndDate = snap.EndDate,
                RentAmount = snap.RentAmount,
                DueDay = snap.DueDay,
                PaymentMethods = snap.PaymentMethods,
                LateFeePolicy = snap.LateFeePolicy,
                SecurityDeposit = snap.SecurityDeposit,
                DepositReturnDays = snap.DepositReturnDays,
                MaxOccupants = snap.MaxOccupants,
                MaxBikeAllowance = snap.MaxBikeAllowance,
                Tenants = snap.Tenants?.ToList() ?? new(),
                Status = ContractStatus.Active,
                PdfUrl = snap.PdfUrl
            };
        }

        private static string ComputeEditHash(Contract c)
        {
            var payload = string.Join("|", new[]
            {
                c.StartDate.ToString("O"),
                c.EndDate.ToString("O"),
                c.DueDay.ToString(),
                c.RentAmount.ToString("0.####"),
                c.SecurityDeposit.ToString("0.####"),
                c.DepositReturnDays.ToString(),
                c.MaxOccupants.ToString(),
                c.MaxBikeAllowance.ToString(),
                c.PaymentMethods ?? "",
                c.LateFeePolicy ?? "",
                c.PropertyDescription ?? ""
            });

            using var sha1 = SHA1.Create();
            var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(bytes);
        }

        private LandlordInfo BuildLandlordInfo()
        {
            var admin = _auth.CurrentAdmin;
            var fullName = string.IsNullOrWhiteSpace(admin?.FullName) ? "Chủ nhà" : admin!.FullName;
            var phone = admin?.PhoneNumber ?? "";
            var address = Editable.HouseAddress;
            var idCard = admin?.IdCardNumber ?? "";
            return new LandlordInfo(fullName, address, idCard, phone);
        }
    }
}