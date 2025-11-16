using AMS.Models;
using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AMS.ViewModels
{
    public partial class PaymentFeesViewModel : ObservableObject
    {
        private readonly IPaymentsRepository _repo;

        [ObservableProperty] private ObservableCollection<FeeType> feeTypes = new();
        [ObservableProperty] private ObservableCollection<RoomCharge> roomCharges = new();

        [ObservableProperty] private decimal defaultElectricRate;
        [ObservableProperty] private decimal defaultWaterRate;
        [ObservableProperty] private decimal defaultInternetFlat;
        [ObservableProperty] private decimal defaultCleaningFlat;

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand AddFeeTypeCommand { get; }
        public IAsyncRelayCommand SaveFeeTypesCommand { get; }
        public IAsyncRelayCommand<RoomCharge> AddFeeToRoomCommand { get; }
        public IAsyncRelayCommand SaveDefaultFeesCommand { get; }

        public PaymentFeesViewModel(IPaymentsRepository repo)
        {
            _repo = repo;

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            AddFeeTypeCommand = new AsyncRelayCommand(AddFeeTypeAsync);
            SaveFeeTypesCommand = new AsyncRelayCommand(SaveFeeTypesAsync);
            AddFeeToRoomCommand = new AsyncRelayCommand<RoomCharge>(AddFeeToRoomAsync);
            SaveDefaultFeesCommand = new AsyncRelayCommand(SaveDefaultFeesAsync);
        }

        private async Task LoadAsync()
        {
            // TODO: load FeeTypes and RoomCharges
            await Task.CompletedTask;
        }

        private async Task AddFeeTypeAsync()
        {
            FeeTypes.Add(new FeeType { Name = "Loại phí mới", DefaultRate = 0 });
            await Task.CompletedTask;
        }

        private async Task SaveFeeTypesAsync()
        {
            // TODO: persist fee types
            await Task.CompletedTask;
        }

        private async Task AddFeeToRoomAsync(RoomCharge? rc)
        {
            if (rc == null) return;
            // TODO: show dialog to pick FeeType and quantity then add FeeInstance
            await Task.CompletedTask;
        }

        private async Task SaveDefaultFeesAsync()
        {
            // TODO persist default rates to settings or PaymentSettings repo
            await Task.CompletedTask;
        }
    }
}