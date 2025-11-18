using AMS.ViewModels;
using Microsoft.Maui.Controls;

namespace AMS.Views
{
    public partial class PaymentSettingsPage : ContentPage
    {
        public PaymentSettingsPage(PaymentSettingsViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is PaymentSettingsViewModel vm)
                await vm.LoadAsync();
        }
    }
}