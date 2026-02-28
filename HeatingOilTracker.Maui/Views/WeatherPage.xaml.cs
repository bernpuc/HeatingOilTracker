using HeatingOilTracker.Maui.ViewModels;

namespace HeatingOilTracker.Maui.Views;

public partial class WeatherPage : ContentPage
{
    private readonly WeatherViewModel _viewModel;

    public WeatherPage(WeatherViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.LoadWeatherAsync();
    }
}