using Microsoft.Maui.Controls;

namespace AMS.Views
{
    public partial class PaymentsPage : ContentPage
    {
        public PaymentsPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is IAppearingAware vm && vm.LoadOnAppear)
                await vm.OnAppearAsync();
        }
    }

    // Optional pattern to avoid compile errors; implement on VMs you want auto-load
    public interface IAppearingAware
    {
        bool LoadOnAppear { get; }
        Task OnAppearAsync();
    }
}