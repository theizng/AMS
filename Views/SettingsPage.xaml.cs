// SettingsPage.xaml.cs
using AMS.ViewModels;
namespace AMS.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}