using HeatingOilTracker.Maui.ViewModels;

namespace HeatingOilTracker.Maui.Views;

public partial class ReferencePage : ContentPage
{
    public ReferencePage(ReferenceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}