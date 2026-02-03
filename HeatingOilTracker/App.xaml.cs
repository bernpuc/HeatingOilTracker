using HeatingOilTracker.Services;
using HeatingOilTracker.ViewModels;
using HeatingOilTracker.Views;
using Prism.Ioc;
using Prism.Navigation.Regions;
using System.Windows;

namespace HeatingOilTracker;

public partial class App : PrismApplication
{
    protected override Window CreateShell()
    {
        var window = Container.Resolve<MainWindow>();
        var vm = Container.Resolve<MainWindowViewModel>();
        window.DataContext = vm;
        return window;
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Shell
        containerRegistry.Register<MainWindow>();
        containerRegistry.Register<MainWindowViewModel>();

        // Services
        containerRegistry.RegisterSingleton<IDataService, DataService>();
        containerRegistry.Register<ICsvImportService, CsvImportService>();
        containerRegistry.Register<IWeatherService, WeatherService>();
        containerRegistry.Register<ITankEstimatorService, TankEstimatorService>();
        containerRegistry.Register<IReportService, ReportService>();

        // Views for navigation
        containerRegistry.RegisterForNavigation<DashboardView, DashboardViewModel>();
        containerRegistry.RegisterForNavigation<DeliveriesView, DeliveriesViewModel>();
        containerRegistry.RegisterForNavigation<ChartsView, ChartsViewModel>();
        containerRegistry.RegisterForNavigation<ReportsView, ReportsViewModel>();
        containerRegistry.RegisterForNavigation<SettingsView, SettingsViewModel>();
        containerRegistry.RegisterForNavigation<ReferenceView, ReferenceViewModel>();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        var regionManager = Container.Resolve<IRegionManager>();
        regionManager.RequestNavigate("ContentRegion", "DashboardView");

        // Fetch latest weather data in background
        _ = FetchLatestWeatherAsync();
    }

    /// <summary>
    /// Automatically fetches weather data from the last known date to today.
    /// Runs silently in the background on startup.
    /// </summary>
    private async Task FetchLatestWeatherAsync()
    {
        try
        {
            var dataService = Container.Resolve<IDataService>();
            var weatherService = Container.Resolve<IWeatherService>();

            // Check if location is configured
            var location = await dataService.GetLocationAsync();
            if (!location.IsSet)
                return;

            // Get existing weather history
            var weatherHistory = await dataService.GetWeatherHistoryAsync();
            var deliveries = await dataService.GetDeliveriesAsync();

            // Determine start date for fetching
            DateTime startDate;
            if (weatherHistory.Count > 0)
            {
                // Start from the day after the last weather record
                var lastWeatherDate = weatherHistory.Max(w => w.Date);
                startDate = lastWeatherDate.AddDays(1);
            }
            else if (deliveries.Count > 0)
            {
                // No weather data yet - start from first delivery
                startDate = deliveries.Min(d => d.Date).AddDays(-1);
            }
            else
            {
                // No deliveries yet, nothing to fetch
                return;
            }

            // Only fetch if we're missing data (start date is before today)
            if (startDate.Date >= DateTime.Today)
                return;

            var endDate = DateTime.Today;

            // Fetch and save new weather data
            var newWeatherData = await weatherService.GetHistoricalWeatherAsync(
                location.Latitude, location.Longitude, startDate, endDate);

            if (newWeatherData.Count > 0)
            {
                await dataService.AddWeatherDataAsync(newWeatherData);
                System.Diagnostics.Debug.WriteLine($"Auto-fetched {newWeatherData.Count} days of weather data");
            }
        }
        catch (Exception ex)
        {
            // Silently log errors - don't interrupt app startup
            System.Diagnostics.Debug.WriteLine($"Auto weather fetch failed: {ex.Message}");
        }
    }
}
