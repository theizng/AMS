using AMS.ViewModels;

namespace AMS.Views;

[QueryProperty(nameof(HouseId), "houseId")]
public partial class RoomsPage : ContentPage
{
    public string? HouseId
    {
        set
        {
            if (BindingContext is RoomsViewModel vm && int.TryParse(value, out int id))
            {
                vm.SetHouseId(id);
            }
        }
    }

    public RoomsPage(RoomsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}