using AMS.ViewModels;
using Microsoft.Maui.Controls;

namespace AMS.Views
{
    public partial class ReportsPage : ContentPage
    {
        public ReportsPage(ReportsViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm; // wire VM -> UI
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is ReportsViewModel vm && vm.LoadCommand.CanExecute(null))
                await vm.LoadCommand.ExecuteAsync(null);
        }
    }
}