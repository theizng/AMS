
using AMS.ViewModels;

namespace AMS.Views
{
    public partial class PaymentsPage : ContentPage
    {
        public PaymentsPage(PaymentsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
