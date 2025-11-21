using AMS.ViewModels;

namespace AMS.Views;

public partial class ReportUtilitiesPage : ContentPage
{
    public ReportUtilitiesPage(ReportUtilitiesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm; // wire VM -> UI
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ReportUtilitiesViewModel vm && vm.LoadCommand.CanExecute(null))
            await vm.LoadCommand.ExecuteAsync(null);
    }
}