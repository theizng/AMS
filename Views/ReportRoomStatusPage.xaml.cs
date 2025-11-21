using AMS.ViewModels;

namespace AMS.Views;

public partial class ReportRoomStatusPage : ContentPage
{
    public ReportRoomStatusPage(ReportRoomStatusSimpleViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ReportRoomStatusSimpleViewModel vm && vm.LoadCommand.CanExecute(null))
            await vm.LoadCommand.ExecuteAsync(null);
    }
}