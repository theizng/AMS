using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace AMS.ViewModels
{
    // Extend your existing PaymentsViewModel
    public partial class PaymentsViewModel
    {
        public IAsyncRelayCommand<object?> UpdateStatusCommand { get; }

        private void InitializeUpdateStatusCommand()
        {
            // Call this from constructor after other commands are initialized
            if (UpdateStatusCommand == null)
            {
                UpdateStatusCommandBacking = new AsyncRelayCommand<object?>(UpdateStatusAsync);
            }
        }

        // Backing field to allow null-check above without changing ctor if needed
        private IAsyncRelayCommand<object?> UpdateStatusCommandBacking
        {
            get => UpdateStatusCommand;
            set { /* hook into existing property if using [ObservableProperty], else expose as normal prop */ }
        }

        private async Task UpdateStatusAsync(object? row)
        {
            if (row is null) return;

            var choice = await Shell.Current.DisplayActionSheet(
                "Cập nhật trạng thái",
                "Hủy",
                null,
                "Đã trả",
                "Trả một phần",
                "Chưa trả");

            if (string.IsNullOrEmpty(choice) || choice == "Hủy")
                return;

            // Try to execute existing commands with the row as parameter
            switch (choice)
            {
                case "Đã trả":
                    TryExecute(MarkPaidCommand, row);
                    break;

                case "Trả một phần":
                    TryExecute(AddPartialPaymentCommand, row);
                    break;

                case "Chưa trả":
                    TryExecute(MarkUnpaidCommand, row);
                    break;
            }
        }

        private static async void TryExecute(ICommand? command, object param)
        {
            if (command is IAsyncRelayCommand asyncCmdNoGen)
            {
                await asyncCmdNoGen.ExecuteAsync(param);
                return;
            }
            if (command is IAsyncRelayCommand<object?> asyncCmd)
            {
                await asyncCmd.ExecuteAsync(param);
                return;
            }
            command?.Execute(param);
        }
    }
}