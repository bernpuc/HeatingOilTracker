using HeatingOilTracker.Maui.ViewModels;

namespace HeatingOilTracker.Maui.Views;

public partial class DeliveryEditorPage : ContentPage
{
    public DeliveryEditorPage(DeliveryEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}