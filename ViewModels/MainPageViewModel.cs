using AMS.Data;
using AMS.Services;
using AMS.Views;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
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

        public DateTime CurrentDateTime
        {
            get => _currentDateTime;
            set
            {
                _currentDateTime = value;
                OnPropertyChanged();
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

        public int PendingMaintainence
        {
            get => _pendingMaintenance;
            set {
                _pendingMaintenance = value;
                OnPropertyChanged();
            }
        }

        public int CurrentMonth         {
            get => _currentMonth.Month;
            set
            {
                _currentMonth = DateTime.Now;
                OnPropertyChanged();
            }
        }


        //Command để điều hướng
        public ICommand NavigateToCommand { get; }
        public ICommand RefreshCommand { get; }

        public MainPageViewModel(IAuthService authService, AMSDbContext dbContext)
        {
            _authService = authService;
            _dbContext = dbContext;

            // Khởi tạo các giá trị ban đầu
            _currentDateTime = DateTime.Now;
            if(!(_authService.CurrentAdmin is null))
            {
                CurrentUser = _authService.CurrentAdmin.FullName;
            }
            else
            {
                CurrentUser = "Chưa đăng nhập";
                Application.Current.MainPage = new LoginShell();
            }


                // Load dữ liệu ban đầu
                //LoadDashboardData();

                // Cài đặt timer để cập nhật thời gian hiện tại mỗi phút
            _timer = new System.Timers.Timer(1000); // 1 giây
            _timer.Elapsed += (s, e) => Device.BeginInvokeOnMainThread(() => CurrentDateTime = DateTime.Now);
            _timer.Start();
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
