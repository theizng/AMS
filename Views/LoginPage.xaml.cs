namespace AMS.Views;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();

		this.BindingContext = new ViewModels.LoginViewModel();
    }
}