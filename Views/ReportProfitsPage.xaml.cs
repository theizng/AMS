using AMS.ViewModels;

namespace AMS.Views;

public partial class ReportProfitsPage : ContentPage
{
    public ReportProfitsPage(ReportProfitsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ReportProfitsViewModel vm && vm.LoadCommand.CanExecute(null))
            await vm.LoadCommand.ExecuteAsync(null);
    }
}