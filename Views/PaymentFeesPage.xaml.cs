using AMS.ViewModels;
using Microsoft.Maui.Controls;

namespace AMS.Views
{
    public partial class PaymentFeesPage : ContentPage
    {
        public PaymentFeesPage(PaymentFeesViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is PaymentFeesViewModel vm)
                await vm.LoadAsync();
        }
    }
}