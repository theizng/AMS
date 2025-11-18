using AMS.ViewModels;
using Microsoft.Maui.Controls;

namespace AMS.Views
{
    public partial class PaymentInvoicesPage : ContentPage
    {
        public PaymentInvoicesPage(PaymentInvoicesViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is PaymentInvoicesViewModel vm)
                await vm.LoadAsync();
        }
    }
}