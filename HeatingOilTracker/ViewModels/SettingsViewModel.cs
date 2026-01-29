using HeatingOilTracker.Models;
using HeatingOilTracker.Services;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System.Windows;

namespace HeatingOilTracker.ViewModels;

public class SettingsViewModel : BindableBase, INavigationAware
{
    private readonly IDataService _dataService;
    private readonly IWeatherService _weatherService;

    private decimal _tankCapacity;
    private string _dataFilePath = string.Empty;
    private string _zipCode = string.Empty;
    private string _locationDisplay = "Not set";
    private bool _isLookingUpZip;
    private bool _isFetchingWeather;
    private string _weatherStatus = string.Empty;

    public decimal TankCapacity
    {
        get => _tankCapacity;
        set => SetProperty(ref _tankCapacity, value);
    }

    public string DataFilePath
    {
        get => _dataFilePath;
        set => SetProperty(ref _dataFilePath, value);
    }

    public string ZipCode
    {
        get => _zipCode;
        set => SetProperty(ref _zipCode, value);
    }

    public string LocationDisplay
    {
        get => _locationDisplay;
        set => SetProperty(ref _locationDisplay, value);
    }

    public bool IsLookingUpZip
    {
        get => _isLookingUpZip;
        set
        {
            SetProperty(ref _isLookingUpZip, value);
            LookupZipCommand.RaiseCanExecuteChanged();
        }
    }

    public bool IsFetchingWeather
    {
        get => _isFetchingWeather;
        set
        {
            SetProperty(ref _isFetchingWeather, value);
            FetchWeatherCommand.RaiseCanExecuteChanged();
        }
    }

    public string WeatherStatus
    {
        get => _weatherStatus;
        set => SetProperty(ref _weatherStatus, value);
    }

    public DelegateCommand SaveCommand { get; }
    public DelegateCommand LookupZipCommand { get; }
    public DelegateCommand FetchWeatherCommand { get; }

    public SettingsViewModel(IDataService dataService, IWeatherService weatherService)
    {
        _dataService = dataService;
        _weatherService = weatherService;
        DataFilePath = _dataService.GetDataFilePath();

        SaveCommand = new DelegateCommand(async () => await SaveAsync());
        LookupZipCommand = new DelegateCommand(
            async () => await LookupZipAsync(),
            () => !IsLookingUpZip && !string.IsNullOrWhiteSpace(ZipCode))
            .ObservesProperty(() => ZipCode);
        FetchWeatherCommand = new DelegateCommand(
            async () => await FetchWeatherAsync(),
            () => !IsFetchingWeather);

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        TankCapacity = await _dataService.GetTankCapacityAsync();

        var location = await _dataService.GetLocationAsync();
        if (location.IsSet)
        {
            LocationDisplay = location.DisplayName;
        }

        var weather = await _dataService.GetWeatherHistoryAsync();
        if (weather.Count > 0)
        {
            var minDate = weather.Min(w => w.Date);
            var maxDate = weather.Max(w => w.Date);
            WeatherStatus = $"{weather.Count} days of data ({minDate:MMM d, yyyy} - {maxDate:MMM d, yyyy})";
        }
        else
        {
            WeatherStatus = "No weather data";
        }
    }

    private async Task SaveAsync()
    {
        if (TankCapacity <= 0)
        {
            MessageBox.Show("Tank capacity must be greater than zero.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await _dataService.SetTankCapacityAsync(TankCapacity);
        MessageBox.Show("Settings saved.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async Task LookupZipAsync()
    {
        if (string.IsNullOrWhiteSpace(ZipCode)) return;

        IsLookingUpZip = true;
        try
        {
            var location = await _weatherService.GeocodeZipCodeAsync(ZipCode.Trim());
            if (location != null)
            {
                await _dataService.SetLocationAsync(location);
                LocationDisplay = location.DisplayName;
                MessageBox.Show($"Location set to {location.DisplayName}", "Location Set",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Could not find that ZIP code. Please check and try again.",
                    "ZIP Code Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error looking up ZIP code: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLookingUpZip = false;
        }
    }

    private async Task FetchWeatherAsync()
    {
        var location = await _dataService.GetLocationAsync();
        if (!location.IsSet)
        {
            MessageBox.Show("Please set a location first by entering a ZIP code.",
                "Location Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var deliveries = await _dataService.GetDeliveriesAsync();
        if (deliveries.Count == 0)
        {
            MessageBox.Show("No deliveries found. Add deliveries first to fetch relevant weather data.",
                "No Deliveries", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        IsFetchingWeather = true;
        WeatherStatus = "Fetching weather data...";

        try
        {
            // Fetch weather from first delivery to today
            var startDate = deliveries.Min(d => d.Date).AddDays(-1);
            var endDate = DateTime.Today;

            var weatherData = await _weatherService.GetHistoricalWeatherAsync(
                location.Latitude, location.Longitude, startDate, endDate);

            if (weatherData.Count > 0)
            {
                await _dataService.AddWeatherDataAsync(weatherData);
                WeatherStatus = $"{weatherData.Count} days fetched ({startDate:MMM d, yyyy} - {endDate:MMM d, yyyy})";
                MessageBox.Show($"Successfully fetched {weatherData.Count} days of weather data.",
                    "Weather Data", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                WeatherStatus = "No data returned from API";
                MessageBox.Show("No weather data was returned. Please try again later.",
                    "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            WeatherStatus = "Error fetching weather";
            MessageBox.Show($"Error fetching weather data: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsFetchingWeather = false;
        }
    }

    public void OnNavigatedTo(NavigationContext navigationContext) => _ = LoadAsync();
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }
}
