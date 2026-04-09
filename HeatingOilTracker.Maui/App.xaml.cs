using HeatingOilTracker.Core.Interfaces;

namespace HeatingOilTracker.Maui;

public partial class App : Application
{
    private readonly IDataService _dataService;
    private readonly IWeatherService _weatherService;
    private readonly ISyncService? _syncService;
    private DateTime _lastRefreshDate = DateTime.MinValue;
    private DateTime _lastSyncAt = DateTime.MinValue;
    private static readonly TimeSpan SyncOnResumeCooldown = TimeSpan.FromMinutes(30);
    private IDispatcherTimer? _refreshTimer;

    public App(IDataService dataService, IWeatherService weatherService, ISyncService? syncService = null)
    {
        InitializeComponent();
        _dataService = dataService;
        _weatherService = weatherService;
        _syncService = syncService;

        _ = SyncOnStartupAsync();
        _lastSyncAt = DateTime.UtcNow;
        _ = FetchLatestWeatherAsync();

        _lastRefreshDate = DateTime.Today;
        StartRefreshTimer();
    }

    private async Task SyncOnStartupAsync()
    {
        // Do NOT check IsSignedIn here — it is populated by a fire-and-forget
        // call in GoogleDriveSyncService's constructor and may not be set yet.
        // SyncOnStartupAsync will return SyncStatus.NotSignedIn if no token exists.
        if (_syncService is null) return;

        try
        {
            var localData = await _dataService.LoadAsync();
            var result = await _syncService.SyncOnStartupAsync(localData);
            if (result.Status == SyncStatus.Success)
            {
                await _dataService.SaveAsync(result.MergedData);
                _dataService.InvalidateCache();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Sync] Startup sync failed: {ex.Message}");
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
#if WINDOWS
        window.HandlerChanged += (s, e) =>
        {
            if (window.Handler?.PlatformView is Microsoft.UI.Xaml.Window winUiWindow)
                winUiWindow.DispatcherQueue.TryEnqueue(
                    Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                    () => winUiWindow.ExtendsContentIntoTitleBar = false);
        };
#endif
        return window;
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
        // Sync on resume if enough time has passed — catches deliveries whose auto-push
        // failed silently while the app was in the background.
        if (DateTime.UtcNow - _lastSyncAt >= SyncOnResumeCooldown)
        {
            System.Diagnostics.Debug.WriteLine("[Sync] App resumed after cooldown - syncing");
            _ = SyncOnStartupAsync();
            _lastSyncAt = DateTime.UtcNow;
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

                // Also backfill if deliveries predate the oldest stored weather record
                if (deliveries.Count > 0)
                {
                    var earliestDelivery = deliveries.Min(d => d.Date).AddDays(-1);
                    var oldestWeather = weatherHistory.Min(w => w.Date);
                    if (earliestDelivery < oldestWeather)
                        startDate = earliestDelivery;
                }
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