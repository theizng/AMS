using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AMS.Data;
using AMS.Models;
using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Linq;
using System.Collections.Generic;

namespace AMS.ViewModels
{


    public class MainPageViewModel : INotifyPropertyChanged
    {
        private readonly IAuthService _authService;
        private readonly AMSDbContext _dbContext;
        private readonly IOnlineMaintenanceReader _onlineReader;
        private readonly IPaymentsRepository _payments;
        private readonly IPaymentSettingsProvider _paymentSettings;

        private DateTime _currentDateTime;
        private string _currentUser = "Chưa đăng nhập";
        private int _totalRooms;
        private int _occupiedRooms;
        private int _inactiveRooms;
        private decimal _monthlyRevenue;
        private decimal _currentDebt;
        private int _debtRooms;
        private int _pendingMaintenance;
        private DateTime _lastMonthDate;

        private decimal _occupancyRate;
        private int _expiringContractsCount;
        private int _addendumNeededCount;
        private int _latePaymentRooms;
        private int _tenantCount;
        private string _upcomingDueReminder = "";

        private System.Timers.Timer _timer;

        private bool _isLoading;
        private CancellationTokenSource? _loadCts;

        public string CurrentUser
        {
            get => _currentUser;
            set { _currentUser = value; OnPropertyChanged(); }
        }

        public DateTime CurrentDateTime
        {
            get => _currentDateTime;
            set { _currentDateTime = value; OnPropertyChanged(); }
        }

        public int TotalRooms
        {
            get => _totalRooms;
            set { _totalRooms = value; OnPropertyChanged(); UpdateOccupancyRate(); }
        }

        public int OccupiedRooms
        {
            get => _occupiedRooms;
            set { _occupiedRooms = value; OnPropertyChanged(); UpdateOccupancyRate(); }
        }

        public int InactiveRooms
        {
            get => _inactiveRooms;
            set { _inactiveRooms = value; OnPropertyChanged(); }
        }

        public decimal MonthlyRevenue
        {
            get => _monthlyRevenue;
            set { _monthlyRevenue = value; OnPropertyChanged(); }
        }

        public decimal CurrentDebt
        {
            get => _currentDebt;
            set { _currentDebt = value; OnPropertyChanged(); }
        }

        public int DebtRooms
        {
            get => _debtRooms;
            set { _debtRooms = value; OnPropertyChanged(); }
        }

        public int PendingMaintenance
        {
            get => _pendingMaintenance;
            set { _pendingMaintenance = value; OnPropertyChanged(); }
        }

        public DateTime LastMonthDate
        {
            get => _lastMonthDate;
            set { _lastMonthDate = value; OnPropertyChanged(); }
        }

        public decimal OccupancyRate
        {
            get => _occupancyRate;
            private set { _occupancyRate = value; OnPropertyChanged(); }
        }

        public int ExpiringContractsCount
        {
            get => _expiringContractsCount;
            private set { _expiringContractsCount = value; OnPropertyChanged(); }
        }

        public int AddendumNeededCount
        {
            get => _addendumNeededCount;
            private set { _addendumNeededCount = value; OnPropertyChanged(); }
        }

        public int LatePaymentRooms
        {
            get => _latePaymentRooms;
            private set { _latePaymentRooms = value; OnPropertyChanged(); }
        }

        public int TenantCount
        {
            get => _tenantCount;
            private set { _tenantCount = value; OnPropertyChanged(); }
        }

        public string UpcomingDueReminder
        {
            get => _upcomingDueReminder;
            private set { _upcomingDueReminder = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand NavigateToCommand { get; }
        public ICommand RefreshCommand { get; }

        // NEW: public reload entry
        public Task ReloadAsync() => LoadDashboardDataInternalAsync(force: true);

        public MainPageViewModel(
            IAuthService authService,
            AMSDbContext dbContext,
            IOnlineMaintenanceReader onlineReader,
            IPaymentsRepository payments,
            IPaymentSettingsProvider paymentSettings)
        {
            _authService = authService;
            _dbContext = dbContext;
            _onlineReader = onlineReader;
            _payments = payments;
            _paymentSettings = paymentSettings;

            NavigateToCommand = new Command<string>(OnNavigateTo);
            RefreshCommand = new Command(async () => await LoadDashboardData());

            _currentDateTime = DateTime.Now;
            LastMonthDate = new DateTime(_currentDateTime.Year, _currentDateTime.Month, 1).AddMonths(-1);

            if (_authService.CurrentAdmin != null)
            {
                CurrentUser = _authService.CurrentAdmin.FullName;
            }
            else
            {
                Application.Current.MainPage = new LoginShell();
                return;
            }

            _ = LoadDashboardData();

            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() => CurrentDateTime = DateTime.Now);
            };
            _timer.Start();

        }

        // Replace previous private LoadDashboardData() with wrapper:
        private Task LoadDashboardData() => LoadDashboardDataInternalAsync(force: false);

        private async Task LoadDashboardDataInternalAsync(bool force)
        {
            if (_isLoading && !force) return;

            // cancel previous if force reload requested
            if (force && _isLoading)
            {
                try { _loadCts?.Cancel(); } catch { }
            }

            _loadCts = new CancellationTokenSource();
            var ct = _loadCts.Token;
            _isLoading = true;
            try
            {
                TotalRooms = await _dbContext.Rooms.CountAsync(cancellationToken: ct);
                OccupiedRooms = await _dbContext.Rooms.CountAsync(r => r.RoomStatus == Room.Status.Occupied, ct);
                InactiveRooms = await _dbContext.Rooms.CountAsync(r => r.RoomStatus == Room.Status.Inactive, ct);

                PendingMaintenance = await GetNewMaintenanceCountAsync();

                await LoadFinancialMetricsAsync();
                await LoadContractMetricsAsync();
                await LoadLatePaymentRoomsAsync();
                LoadDueReminder();

                LastMonthDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
            }
            catch (OperationCanceledException) { /* ignore */ }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dashboard load error: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task LoadFinancialMetricsAsync()
        {
            var today = DateTime.Today;

            // Last month revenue
            var lastMonth = LastMonthDate.Month;
            var lastYear = LastMonthDate.Year;
            var lastCycle = await _payments.GetCycleAsync(lastYear, lastMonth);
            if (lastCycle != null)
            {
                var lastCharges = await _payments.GetRoomChargesForCycleAsync(lastCycle.CycleId);
                MonthlyRevenue = lastCharges.Sum(rc => rc.TotalDue);
            }
            else
            {
                MonthlyRevenue = 0m;
            }

            // Current debt (amount remaining this month)
            var currentCycle = await _payments.GetCycleAsync(today.Year, today.Month);
            if (currentCycle != null)
            {
                var currentCharges = await _payments.GetRoomChargesForCycleAsync(currentCycle.CycleId);
                CurrentDebt = currentCharges.Sum(rc => rc.AmountRemaining);
                DebtRooms = currentCharges.Count(rc => rc.AmountRemaining > 0);
            }
            else
            {
                CurrentDebt = 0m;
                DebtRooms = 0;
            }
        }

        private async Task LoadContractMetricsAsync()
        {
            var activeContracts = await _dbContext.Contracts
                .Where(c => c.Status == ContractStatus.Active)
                .ToListAsync();

            ExpiringContractsCount = activeContracts.Count(c => c.IsExpiringSoon(30));
            AddendumNeededCount = activeContracts.Count(c => c.NeedsAddendum);
            TenantCount = activeContracts.Sum(c => c.Tenants?.Count ?? 0);
        }

        private async Task LoadLatePaymentRoomsAsync()
        {
            var today = DateTime.Today;
            var cycle = await _payments.GetCycleAsync(today.Year, today.Month);
            if (cycle == null)
            {
                LatePaymentRooms = 0;
                return;
            }
            var charges = await _payments.GetRoomChargesForCycleAsync(cycle.CycleId);
            LatePaymentRooms = charges.Count(rc => rc.Status == PaymentStatus.Late);
        }

        private void LoadDueReminder()
        {
            try
            {
                var settings = _paymentSettings.Get();
                var today = DateTime.Today;
                var dueDay = Math.Clamp(settings.DefaultDueDay, 1, 28);
                var dueDate = new DateTime(today.Year, today.Month, dueDay);
                var graceDate = dueDate.AddDays(settings.GraceDays);

                if (today < dueDate)
                    UpcomingDueReminder = $"Chưa đến ngày thu phí. Ngày thu: {dueDay:00}/{today:MM}.";
                else if (today <= graceDate)
                    UpcomingDueReminder = "Đã đến ngày thu phí. Vui lòng ghi chỉ số và chuẩn bị gửi hóa đơn.";
                else
                    UpcomingDueReminder = "Đã quá hạn thu phí (hết ân hạn). Nên gửi nhắc nợ các phòng chưa thanh toán.";
            }
            catch
            {
                UpcomingDueReminder = "Không lấy được cấu hình ngày thu phí.";
            }
        }

        private void UpdateOccupancyRate()
        {
            OccupancyRate = TotalRooms == 0 ? 0 : Math.Round((decimal)OccupiedRooms / TotalRooms * 100, 2);
        }

        private async Task<int> GetNewMaintenanceCountAsync()
        {
            try
            {
                var url = Preferences.Get("maintenance:sheet:url", null);
                if (string.IsNullOrWhiteSpace(url)) return 0;

                var items = await _onlineReader.ReadFromUrlAsync(url);
                return items.Count(m => m.Status == MaintenanceStatus.New);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] Maintenance load failed: {ex.Message}");
                return 0;
            }
        }

        private async void OnNavigateTo(string? input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input)) return;

                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["RoomsPage"] = "rooms",
                    ["rooms"] = "rooms",
                    ["TenantsPage"] = "tenants",
                    ["tenants"] = "tenants",
                    // keep synonyms if ever used
                    ["PaymentsPage"] = "payment_overview",
                    ["ReportsPage"] = "report"
                };

                if (!map.TryGetValue(input, out var route))
                    route = input;

                // include Shell top-level routes for absolute navigation
                var shellTree = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "MainPage", "houses", "tenants", "maintenances", "contracts",
                  "payment_overview", "payment_meterentry", "payment_fees", "payment_invoices", "payment_settings",
                  "report", "report_revenue", "report_profit", "report_utilities", "report_roomstatus", "report_debt",
                  "settings" };

                if (shellTree.Contains(route))
                    await Shell.Current.GoToAsync($"//{route}");
                else
                    await Shell.Current.GoToAsync(route);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // Ensure Dispose cancels CTS
        ~MainPageViewModel()
        {
            try { _loadCts?.Cancel(); } catch { }
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}