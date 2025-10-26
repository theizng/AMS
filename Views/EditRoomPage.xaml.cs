using AMS.ViewModels;

namespace AMS.Views;

    [QueryProperty(nameof(HouseId), "houseId")]
    [QueryProperty(nameof(RoomId), "roomId")]
    public partial class EditRoomPage : ContentPage
    {
        private readonly RoomEditViewModel _vm;

        public string? HouseId
        {
            set
            {
                if (int.TryParse(value, out var id))
                {
                    _vm.SetHouseId(id);
                }
            }
        }

        public string? RoomId
        {
            set
            {
                if (int.TryParse(value, out var id))
                {
                    _vm.SetRoomId(id);
                }
            }
        }

        public EditRoomPage(RoomEditViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
        }
    }