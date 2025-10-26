using AMS.ViewModels;

namespace AMS.Views
{
    [QueryProperty(nameof(TenantId), "tenantId")]
    public partial class EditTenantPage : ContentPage
    {
        public string? TenantId
        {
            set
            {
                if (BindingContext is TenantEditViewModel vm && int.TryParse(value, out int id))
                {
                    vm.SetTenantId(id);
                }
            }
        }

        public EditTenantPage(TenantEditViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}