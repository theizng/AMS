
using AMS.ViewModels;
namespace AMS.Views;

public partial class MainPage : ContentPage
{
	public MainPage(MainPageViewModel viewmodel)
	{
		InitializeComponent();
		BindingContext = viewmodel;
	}
}