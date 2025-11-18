using AMS.ViewModels;
using Microsoft.Maui.Controls;

namespace AMS.Views
{
    public partial class PaymentsPage : ContentPage
    {
        public PaymentsPage(PaymentsViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is PaymentsViewModel vm)
                await vm.LoadCyclesAsync();
        }
    }
}