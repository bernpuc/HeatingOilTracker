using HeatingOilTracker.Core.Interfaces;

namespace HeatingOilTracker.Maui;

public partial class App : Application
{
    private readonly IDataService _dataService;
    private readonly IWeatherService _weatherService;
    private DateTime _lastRefreshDate = DateTime.MinValue;
    private IDispatcherTimer? _refreshTimer;

    public App(IDataService dataService, IWeatherService weatherService)
    {
        InitializeComponent();
        _dataService = dataService;
        _weatherService = weatherService;

        _ = FetchLatestWeatherAsync();

        _lastRefreshDate = DateTime.Today;
        StartRefreshTimer();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }

    private void StartRefreshTimer()
    {
        _refreshTimer = Dispatcher.CreateTimer();
        _refreshTimer.Interval = TimeSpan.FromHours(1);
        _refreshTimer.Tick += OnRefreshTimerTick;
        _refreshTimer.Start();
    }

    private async void OnRefreshTimerTick(object? sender, EventArgs e)
    {
        if (DateTime.Today > _lastRefreshDate)
        {
            System.Diagnostics.Debug.WriteLine("[Weather] Date changed - refreshing");
            await FetchLatestWeatherAsync();
            _lastRefreshDate = DateTime.Today;
        }
    }

    protected override void OnResume()
    {
        base.OnResume();
        if (DateTime.Today > _lastRefreshDate)
        {
            System.Diagnostics.Debug.WriteLine("[Weather] App resumed on new day - refreshing");
            _ = FetchLatestWeatherAsync();
            _lastRefreshDate = DateTime.Today;
        }
    }

    private async Task FetchLatestWeatherAsync()
    {
        try
        {
            var location = await _dataService.GetLocationAsync();
            if (!location.IsSet)
            {
                System.Diagnostics.Debug.WriteLine("[Weather] No location configured, skipping");
                return;
            }

            var weatherHistory = await _dataService.GetWeatherHistoryAsync();
            var deliveries = await _dataService.GetDeliveriesAsync();

            DateTime startDate;
            if (weatherHistory.Count > 0)
            {
                startDate = weatherHistory.Max(w => w.Date).AddDays(1);
            }
            else if (deliveries.Count > 0)
            {
                startDate = deliveries.Min(d => d.Date).AddDays(-1);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[Weather] No deliveries yet, skipping");
                return;
            }

            if (startDate.Date >= DateTime.Today)
            {
                System.Diagnostics.Debug.WriteLine("[Weather] Already up to date");
                return;
            }

            var endDate = DateTime.Today;
            System.Diagnostics.Debug.WriteLine($"[Weather] Fetching {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

            var newData = await _weatherService.GetHistoricalWeatherAsync(
                location.Latitude, location.Longitude, startDate, endDate);

            if (newData.Count > 0)
            {
                await _dataService.AddWeatherDataAsync(newData);
                System.Diagnostics.Debug.WriteLine($"[Weather] Saved {newData.Count} new records");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[Weather] No new records returned");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Weather] Fetch failed: {ex.Message}");
        }
    }
}