using AMS.ViewModels;

namespace AMS.Views
{
    public partial class TenantsPage : ContentPage
    {
        public TenantsPage(TenantsViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is TenantsViewModel vm)
            {
                vm.RefreshCommand.Execute(null);
            }
        }
    }
}