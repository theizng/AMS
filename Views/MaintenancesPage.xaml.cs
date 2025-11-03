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
            if (BindingContext is MaintenancesViewModel vm)
            {
                vm.RefreshCommand.Execute(null);
            }
        }
    }
}