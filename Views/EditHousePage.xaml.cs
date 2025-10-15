using AMS.ViewModels;
using Microsoft.Maui.Controls;

namespace AMS.Views
{
    [QueryProperty(nameof(HouseId), "houseId")]
    public partial class EditHousePage : ContentPage
    {
        public string? HouseId
        {
            set
            {
                if (BindingContext is HouseEditViewModel vm && int.TryParse(value, out int id))
                {
                    vm.SetHouseId(id);
                }
            }
        }

        // Constructed via DI
        public EditHousePage(HouseEditViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}