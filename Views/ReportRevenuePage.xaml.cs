using AMS.ViewModels;

namespace AMS.Views
{
    public partial class ReportRevenuePage : ContentPage
    {
        public ReportRevenuePage(ReportRevenueViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm; // wire VM -> UI
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is ReportRevenueViewModel vm && vm.LoadCommand.CanExecute(null))
                await vm.LoadCommand.ExecuteAsync(null);
        }
    }
}