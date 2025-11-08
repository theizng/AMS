using AMS.ViewModels;
using Microsoft.Maui.Controls;

namespace AMS.Views
{
    public partial class ContractsPage : ContentPage
    {
        public ContractsViewModel ViewModel => (ContractsViewModel)BindingContext;

        public ContractsPage(ContractsViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ViewModel.LoadAsync();
        }
    }
}