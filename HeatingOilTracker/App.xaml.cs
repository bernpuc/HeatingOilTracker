using HeatingOilTracker.Events;
using HeatingOilTracker.Services;
using HeatingOilTracker.ViewModels;
using HeatingOilTracker.Views;
using Prism.Events;
using Prism.Ioc;
using Prism.Navigation.Regions;
using System.Windows;
using System.Windows.Threading;

namespace HeatingOilTracker;

public partial class App : PrismApplication
{
    private DispatcherTimer? _refreshTimer;
    private DateTime _lastRefreshDate = DateTime.MinValue;

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
        _ = FetchLatestWeatherAsync(publishRefresh: false);

        // Start the periodic refresh timer (checks every hour)
        _lastRefreshDate = DateTime.Today;
        StartRefreshTimer();

        // Handle window activation for refresh
        if (MainWindow != null)
        {
            MainWindow.Activated += OnMainWindowActivated;
        }
    }

    private void StartRefreshTimer()
    {
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromHours(1)
        };
        _refreshTimer.Tick += OnRefreshTimerTick;
        _refreshTimer.Start();
    }

    private async void OnRefreshTimerTick(object? sender, EventArgs e)
    {
        // Check if the date has changed (new day)
        if (DateTime.Today > _lastRefreshDate)
        {
            System.Diagnostics.Debug.WriteLine("Date changed - refreshing weather and dashboard");
            await FetchLatestWeatherAsync(publishRefresh: true);
            _lastRefreshDate = DateTime.Today;
        }
    }

    private async void OnMainWindowActivated(object? sender, EventArgs e)
    {
        // Refresh if it's a new day since last refresh
        if (DateTime.Today > _lastRefreshDate)
        {
            System.Diagnostics.Debug.WriteLine("Window activated on new day - refreshing");
            await FetchLatestWeatherAsync(publishRefresh: true);
            _lastRefreshDate = DateTime.Today;
        }
    }

    /// <summary>
    /// Automatically fetches weather data from the last known date to today.
    /// Runs silently in the background.
    /// </summary>
    /// <param name="publishRefresh">If true, publishes events to refresh UI components.</param>
    private async Task FetchLatestWeatherAsync(bool publishRefresh = false)
    {
        try
        {
            var dataService = Container.Resolve<IDataService>();
            var weatherService = Container.Resolve<IWeatherService>();

            // Check if location is configured
            var location = await dataService.GetLocationAsync();
            if (!location.IsSet)
            {
                // Even without weather data, refresh dashboard if requested
                if (publishRefresh)
                    PublishDashboardRefresh();
                return;
            }

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
                if (publishRefresh)
                    PublishDashboardRefresh();
                return;
            }

            // Only fetch if we're missing data (start date is before today)
            if (startDate.Date >= DateTime.Today)
            {
                // No new weather data needed, but still refresh dashboard if requested
                if (publishRefresh)
                    PublishDashboardRefresh();
                return;
            }

            var endDate = DateTime.Today;

            // Fetch and save new weather data
            var newWeatherData = await weatherService.GetHistoricalWeatherAsync(
                location.Latitude, location.Longitude, startDate, endDate);

            if (newWeatherData.Count > 0)
            {
                await dataService.AddWeatherDataAsync(newWeatherData);
                System.Diagnostics.Debug.WriteLine($"Auto-fetched {newWeatherData.Count} days of weather data");
            }

            // Publish refresh events
            if (publishRefresh)
            {
                var eventAggregator = Container.Resolve<IEventAggregator>();
                eventAggregator.GetEvent<WeatherDataUpdatedEvent>().Publish();
                eventAggregator.GetEvent<DashboardRefreshRequestedEvent>().Publish();
            }
        }
        catch (Exception ex)
        {
            // Silently log errors - don't interrupt app
            System.Diagnostics.Debug.WriteLine($"Auto weather fetch failed: {ex.Message}");

            // Still try to refresh dashboard even if weather fetch failed
            if (publishRefresh)
                PublishDashboardRefresh();
        }
    }

    private void PublishDashboardRefresh()
    {
        try
        {
            var eventAggregator = Container.Resolve<IEventAggregator>();
            eventAggregator.GetEvent<DashboardRefreshRequestedEvent>().Publish();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to publish refresh event: {ex.Message}");
        }
    }
}
