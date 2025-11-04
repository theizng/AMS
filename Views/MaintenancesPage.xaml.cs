using AMS.ViewModels;

namespace AMS.Views
{
    public partial class MaintenancesPage : ContentPage
    {
        public MaintenancesPage(MaintenancesViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Optionally trigger a refresh when page appears
            if (BindingContext is MaintenancesViewModel vm && !vm.IsRefreshing)
            {
                vm.RefreshCommand.Execute(null);
            }
        }
    }
}