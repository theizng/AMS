using AMS.ViewModels;

namespace AMS.Views;

public partial class ReportDebtPage : ContentPage
{
    public ReportDebtPage(ReportDebtViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ReportDebtViewModel vm && vm.LoadCommand.CanExecute(null))
            await vm.LoadCommand.ExecuteAsync(null);
    }
}