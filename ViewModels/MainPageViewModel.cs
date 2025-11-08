using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using AMS.Data;
using AMS.Models;
using AMS.Services.Interfaces;
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
        private System.Timers.Timer _timer;

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
            set { _totalRooms = value; OnPropertyChanged(); }
        }

        public int OccupiedRooms
        {
            get => _occupiedRooms;
            set { _occupiedRooms = value; OnPropertyChanged(); }
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

        // For label "Tháng {0:MM/yyyy}" (previous month)
        public DateTime LastMonthDate
        {
            get => _lastMonthDate;
            set { _lastMonthDate = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand NavigateToCommand { get; }
        public ICommand RefreshCommand { get; }

        public MainPageViewModel(IAuthService authService, AMSDbContext dbContext, IOnlineMaintenanceReader onlineReader)
        {
            _authService = authService;
            _dbContext = dbContext;
            _onlineReader = onlineReader;

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

        private async Task LoadDashboardData()
        {
            try
            {
                // Rooms (DB)
                TotalRooms = await _dbContext.Rooms.CountAsync();
                OccupiedRooms = await _dbContext.Rooms.CountAsync(r => r.RoomStatus == Room.Status.Occupied);
                InactiveRooms = await _dbContext.Rooms.CountAsync(r => r.RoomStatus == Room.Status.Inactive);

                // Maintenance (Sheet): only "Chưa xử lý" (New)
                PendingMaintenance = await GetNewMaintenanceCountAsync();

                // Leave these placeholders until finance deploys, but show last month label
                if (MonthlyRevenue == 0) MonthlyRevenue = 15000000M;
                if (CurrentDebt == 0) CurrentDebt = 2500000M;
                if (DebtRooms == 0) DebtRooms = 2;

                // Ensure LastMonthDate reflects current clock
                LastMonthDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dashboard: {ex.Message}");
            }
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

                // Map button parameters to actual routes
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["RoomsPage"] = "rooms",
                    ["rooms"] = "rooms",
                    ["TenantsPage"] = "tenants",
                    ["tenants"] = "tenants",
                    ["PaymentsPage"] = "payments",
                    ["payments"] = "payments",
                    ["ReportsPage"] = "reports",
                    ["reports"] = "reports"
                };

                if (!map.TryGetValue(input, out var route))
                    route = input;

                // Shell tree routes (use absolute //navigation)
                var shellTree = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "MainPage", "houses", "tenants", "maintenances", "payments", "reports", "settings" };

                if (shellTree.Contains(route))
                    await Shell.Current.GoToAsync($"//{route}");
                else
                    await Shell.Current.GoToAsync(route); // registered route like "rooms"
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        ~MainPageViewModel()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}