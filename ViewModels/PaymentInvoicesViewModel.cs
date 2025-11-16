using AMS.Models;
using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AMS.ViewModels
{
    public partial class PaymentInvoicesViewModel : ObservableObject
    {
        private readonly IPaymentsRepository _repo;
        private readonly IEmailNotificationService _email;

        [ObservableProperty] private ObservableCollection<RoomCharge> roomCharges = new();

        public IAsyncRelayCommand GenerateInvoicesCommand { get; }
        public IAsyncRelayCommand SendInitialInvoicesCommand { get; }
        public IAsyncRelayCommand ExportSheetsCommand { get; }
        public IAsyncRelayCommand<RoomCharge> PreviewInvoiceCommand { get; }
        public IAsyncRelayCommand<RoomCharge> SendInvoiceCommand { get; }
        public IAsyncRelayCommand SendLateRemindersCommand { get; }

        public PaymentInvoicesViewModel(IPaymentsRepository repo, IEmailNotificationService email)
        {
            _repo = repo;
            _email = email;

            GenerateInvoicesCommand = new AsyncRelayCommand(GenerateInvoicesAsync);
            SendInitialInvoicesCommand = new AsyncRelayCommand(SendInitialInvoicesAsync);
            ExportSheetsCommand = new AsyncRelayCommand(ExportSheetsAsync);
            PreviewInvoiceCommand = new AsyncRelayCommand<RoomCharge>(PreviewInvoiceAsync);
            SendInvoiceCommand = new AsyncRelayCommand<RoomCharge>(SendInvoiceAsync);
            SendLateRemindersCommand = new AsyncRelayCommand(SendLateRemindersAsync);
        }

        private async Task GenerateInvoicesAsync()
        {
            // TODO: compute invoice lines for each RoomCharge and mark ReadyToSend
            await Task.CompletedTask;
        }

        private async Task SendInitialInvoicesAsync()
        {
            // TODO: iterate ReadyToSend => generate export and send
            await Task.CompletedTask;
        }

        private async Task ExportSheetsAsync()
        {
            // TODO: produce XLSX for three sheets and return path
            await Task.CompletedTask;
        }

        private async Task PreviewInvoiceAsync(RoomCharge? rc)
        {
            if (rc == null) return;
            // TODO: show preview page/modal
            await Task.CompletedTask;
        }

        private async Task SendInvoiceAsync(RoomCharge? rc)
        {
            if (rc == null) return;
            // TODO: send single invoice (attach relevant sheet or pdf)
            await Task.CompletedTask;
        }

        private async Task SendLateRemindersAsync()
        {
            // TODO: send late reminders
            await Task.CompletedTask;
        }
    }
}