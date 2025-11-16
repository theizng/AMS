using AMS.ViewModels;
using Microsoft.Maui.Controls;

namespace AMS.Views
{
    public partial class PaymentMeterEntryPage : ContentPage
    {
        public PaymentMeterEntryPage(PaymentMeterEntryViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is PaymentMeterEntryViewModel vm)
                await vm.LoadAsync();
        }
    }
}