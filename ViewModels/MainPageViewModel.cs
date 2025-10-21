using AMS.Data;
using AMS.Services;
using AMS.Views;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using AMS.Models;

namespace AMS.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private readonly IAuthService _authService;
        private readonly AMSDbContext _dbContext;
        private DateTime _currentDateTime;
        private string _currentUser;
        private int _totalRooms;
        private int _occupiedRooms;
        private decimal _monthlyRevenue;
        private decimal _currentDebt;
        private int _debtRooms;
        private int _pendingMaintenance;
        private DateTime _currentMonth;
        private System.Timers.Timer _timer;

        public string CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                OnPropertyChanged();
            }
        }

        public string FormattedDateTime => _currentDateTime.ToString("yyyy-MM-dd HH:mm:ss");

        public DateTime CurrentDateTime
        {
            get => _currentDateTime;
            set
            {
                _currentDateTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedDateTime));
            }
        }

        public int TotalRooms
        {
            get => _totalRooms;
            set
            {
                _totalRooms = value;
                OnPropertyChanged();
            }
        }

        public int OccupiedRooms
        {
            get => _occupiedRooms;
            set
            {
                _occupiedRooms = value;
                OnPropertyChanged();
            }
        }

        public int DebtRooms
        {
            get => _debtRooms;
            set
            {
                _debtRooms = value;
                OnPropertyChanged();
            }
        }

        public decimal MonthlyRevenue
        {
            get => _monthlyRevenue;
            set
            {
                _monthlyRevenue = value;
                OnPropertyChanged();
            }
        }

        public decimal CurrentDebt
        {
            get => _currentDebt;
            set
            {
                _currentDebt = value;
                OnPropertyChanged();
            }
        }

        public int PendingMaintenance
        {
            get => _pendingMaintenance;
            set
            {
                _pendingMaintenance = value;
                OnPropertyChanged();
            }
        }

        public int CurrentMonth
        {
            get => _currentMonth.Month;
            set
            {
                _currentMonth = DateTime.Now;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand NavigateToCommand { get; }
        public ICommand RefreshCommand { get; }

        public MainPageViewModel(IAuthService authService, AMSDbContext dbContext)
        {
            _authService = authService;
            _dbContext = dbContext;

            // Initialize commands
            NavigateToCommand = new Command<string>(OnNavigateTo);
            RefreshCommand = new Command(async () => await LoadDashboardData());

            // Khởi tạo các giá trị ban đầu
            _currentDateTime = DateTime.UtcNow;
            _currentMonth = DateTime.Now;

            // Check if user is logged in
            if (_authService.CurrentAdmin != null)
            {
                CurrentUser = _authService.CurrentAdmin.FullName;
            }
            else
            {
                CurrentUser = "Chưa đăng nhập";
                Application.Current.MainPage = new LoginShell();
                return;
            }

            // Load dữ liệu ban đầu
            LoadDashboardData();

            // Cài đặt timer để cập nhật thời gian hiện tại mỗi giây
            _timer = new System.Timers.Timer(1000); // 1 giây
            _timer.Elapsed += (s, e) => Device.BeginInvokeOnMainThread(() => CurrentDateTime = DateTime.UtcNow);
            _timer.Start();
        }

        private async Task LoadDashboardData()
        {
            try
            {
                // Load room statistics
                TotalRooms = await _dbContext.Rooms.CountAsync();
                OccupiedRooms = await _dbContext.Rooms.CountAsync(r => r.RoomStatus == Room.Status.Occupied);

                // TODO: Calculate actual values when payment/invoice models are ready
                MonthlyRevenue = 15000000M;
                CurrentDebt = 2500000M;
                DebtRooms = 2;
                PendingMaintenance = 3;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dashboard: {ex.Message}");
            }
        }


        private async void OnNavigateTo(string route)
        {
            try
            {
                await Shell.Current.GoToAsync($"//{route}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Cleanup when viewmodel is disposed
        ~MainPageViewModel()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}