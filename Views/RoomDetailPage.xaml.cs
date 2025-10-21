using AMS.ViewModels;

namespace AMS.Views
{
    [QueryProperty(nameof(RoomId), "roomId")]
    public partial class RoomDetailPage : ContentPage
    {
        public string? RoomId
        {
            set
            {
                if (BindingContext is RoomDetailViewModel vm && int.TryParse(value, out int id))
                {
                    vm.SetRoomId(id);
                }
            }
        }

        public RoomDetailPage(RoomDetailViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is RoomDetailViewModel vm)
            {
                vm.RefreshCommand.Execute(null);
            }
        }
    }
}