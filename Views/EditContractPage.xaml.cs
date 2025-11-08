using AMS.ViewModels;
using Microsoft.Maui.Controls;

namespace AMS.Views
{
    public partial class EditContractPage : ContentPage
    {

        public EditContractPage(ContractEditViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

    }
}