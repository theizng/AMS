using AMS.ViewModels;

namespace AMS.Views
{
    [QueryProperty(nameof(RoomId), "roomId")]
    [QueryProperty(nameof(HouseId), "houseId")]
    public partial class EditRoomPage : ContentPage
    {
        public string? RoomId
        {
            set
            {
                if (BindingContext is RoomEditViewModel vm && int.TryParse(value, out int id))
                {
                    vm.SetRoomId(id);
                }
            }
        }

        public string? HouseId
        {
            set
            {
                if (BindingContext is RoomEditViewModel vm && int.TryParse(value, out int id))
                {
                    vm.SetHouseId(id);
                }
            }
        }

        public EditRoomPage(RoomEditViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}