using HeatingOilTracker.Maui.ViewModels;

namespace HeatingOilTracker.Maui.Views;

public partial class ChartsPage : ContentPage
{
    private readonly ChartsViewModel _viewModel;

    public ChartsPage(ChartsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.LoadChartsAsync();
    }
}