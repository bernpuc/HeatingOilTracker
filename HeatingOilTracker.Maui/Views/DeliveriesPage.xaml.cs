using HeatingOilTracker.Maui.ViewModels;

namespace HeatingOilTracker.Maui.Views;

public partial class DeliveriesPage : ContentPage
{
    private readonly DeliveriesViewModel _viewModel;

    public DeliveriesPage(DeliveriesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.LoadDeliveriesAsync();
    }
}