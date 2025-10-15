using AMS.ViewModels;
namespace AMS.Views;

public partial class HousesPage : ContentPage
{
	public HousesPage(HousesViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}