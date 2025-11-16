using AMS.Models;
using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AMS.ViewModels
{
    public partial class PaymentsViewModel : ObservableObject
    {
        private readonly IPaymentsRepository _repo;
        private readonly IEmailNotificationService _email;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private int selectedYear;
        [ObservableProperty] private int selectedMonth;
        [ObservableProperty] private PaymentCycle? currentCycle;
        [ObservableProperty] private ObservableCollection<RoomCharge> roomCharges = new();
        [ObservableProperty] private string searchText = "";

        [ObservableProperty] private decimal totalDue;
        [ObservableProperty] private decimal totalPaid;
        [ObservableProperty] private decimal totalRemaining;
        [ObservableProperty] private int lateCount;

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand CreateCycleCommand { get; }
        public IAsyncRelayCommand<RecordPaymentRequest> RecordPaymentCommand { get; }
        public IAsyncRelayCommand<RoomCharge> MarkPartialCommand { get; }
        public IAsyncRelayCommand<RoomCharge> MarkPaidCommand { get; }
        public IAsyncRelayCommand SendLateRemindersCommand { get; }

        public PaymentsViewModel(IPaymentsRepository repo, IEmailNotificationService email)
        {
            _repo = repo;
            _email = email;

            var now = DateTime.Today;
            SelectedYear = now.Year;
            SelectedMonth = now.Month;

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            CreateCycleCommand = new AsyncRelayCommand(CreateCycleAsync);
            RecordPaymentCommand = new AsyncRelayCommand<RecordPaymentRequest>(RecordPaymentAsync);
            MarkPartialCommand = new AsyncRelayCommand<RoomCharge>(MarkPartialAsync);
            MarkPaidCommand = new AsyncRelayCommand<RoomCharge>(MarkPaidAsync);
            SendLateRemindersCommand = new AsyncRelayCommand(SendLateRemindersAsync);
        }

        private async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                CurrentCycle = await _repo.GetCycleAsync(SelectedYear, SelectedMonth);
                RoomCharges.Clear();
                if (CurrentCycle != null)
                {
                    foreach (var rc in CurrentCycle.RoomCharges.OrderBy(r => r.RoomCode))
                        RoomCharges.Add(rc);
                    RecalcSummary();
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CreateCycleAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                // Create and initialize a cycle
                var cycle = await _repo.CreateCycleAsync(SelectedYear, SelectedMonth);
                // TODO: Initialize RoomCharges from active contracts
                await _repo.SaveCycleAsync(cycle);
                await LoadAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RecordPaymentAsync(RecordPaymentRequest req)
        {
            if (req == null || req.RoomCharge == null) return;
            // TODO: open prompt to get amount, add PaymentRecord and update repo
            await Task.CompletedTask;
        }

        private async Task MarkPartialAsync(RoomCharge? rc)
        {
            if (rc == null) return;
            rc.Status = PaymentStatus.PartiallyPaid;
            await _repo.UpdateRoomChargeAsync(rc);
            RecalcSummary();
        }

        private async Task MarkPaidAsync(RoomCharge? rc)
        {
            if (rc == null) return;
            rc.AmountPaid = rc.TotalDue;
            rc.Status = PaymentStatus.Paid;
            rc.PaidAt = DateTime.UtcNow;
            await _repo.UpdateRoomChargeAsync(rc);
            RecalcSummary();
        }

        private async Task SendLateRemindersAsync()
        {
            if (CurrentCycle == null) return;
            // TODO: collect targets and call _email.SendInvoiceAsync or dedicated reminder
            await Task.CompletedTask;
        }

        private void RecalcSummary()
        {
            TotalDue = RoomCharges.Sum(r => r.TotalDue);
            TotalPaid = RoomCharges.Sum(r => r.AmountPaid);
            TotalRemaining = TotalDue - TotalPaid;
            LateCount = RoomCharges.Count(r => r.Status == PaymentStatus.Late);
        }
    }

    // small helper DTO for RecordPayment command
    public class RecordPaymentRequest
    {
        public RoomCharge? RoomCharge { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
    }
}