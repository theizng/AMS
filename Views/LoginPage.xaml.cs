using AMS.Data;
using AMS.ViewModels;
using Microsoft.EntityFrameworkCore.Storage;
namespace AMS.Views
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage(LoginViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}