using AMS.ViewModels;

namespace AMS.Views;

public partial class ForgotPasswordPage : ContentPage
{
	public ForgotPasswordPage(ForgotPasswordViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}