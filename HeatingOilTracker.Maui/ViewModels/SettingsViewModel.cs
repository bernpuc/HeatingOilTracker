using HeatingOilTracker.Core.Interfaces;
using HeatingOilTracker.Core.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HeatingOilTracker.Maui.Services;

namespace HeatingOilTracker.Maui.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly IDataService _dataService;
    private readonly IWeatherService _weatherService;
    private readonly ISyncService? _syncService;

    private decimal _tankCapacity;
    private string _tankCapacityText = string.Empty;
    private string _locationSearch = string.Empty;
    private string _locationDisplay = "Not set";
    private bool _isSearchingLocation;
    private bool _isFetchingWeather;
    private string _weatherStatus = string.Empty;
    private Core.Models.Location? _selectedSearchResult;
    private bool _reminderEnabled = true;
    private string _thresholdGallonsText = "50";
    private string _thresholdDaysText = string.Empty;
    private string _dataFilePath = string.Empty;

    // Regional settings
    private CultureOption _selectedCulture = SupportedCultures.All[0];
    private TemperatureUnitOption _selectedTemperatureUnit = TemperatureUnits.All[0];
    private FuelTypeOption _selectedFuelType = FuelTypes.All[0];
    private MonthOption _selectedHeatingSeasonStartMonth = MonthOptions.GetByNumber(10);
    private MonthOption _selectedHeatingSeasonEndMonth = MonthOptions.GetByNumber(3);

    // EIA region
    private string _eiaRegionCode = string.Empty;

    // API keys
    private string _eiaApiKey = string.Empty;
    private string _eiaKeyStatus = string.Empty;

    // Sync state
    private string _syncStatusText = "Not connected";
    private bool _isSyncing;
    private bool _isSignedIn;
    private string _syncAccountText = string.Empty;
    private string _lastSyncedText = string.Empty;

    // EIA region picker options — empty code = auto-detect
    public List<Core.Models.EiaRegionOption> EiaRegions { get; } =
        new[] { new Core.Models.EiaRegionOption("", "Auto-detect from location") }
        .Concat(Core.Models.EiaRegion.All.Select(r => new Core.Models.EiaRegionOption(r.Code, r.Name)))
        .ToList();

    private Core.Models.EiaRegionOption? _selectedEiaRegion;
    public Core.Models.EiaRegionOption? SelectedEiaRegion
    {
        get => _selectedEiaRegion;
        set
        {
            SetProperty(ref _selectedEiaRegion, value);
            EiaRegionCode = value?.Code ?? string.Empty;
        }
    }

    public string EiaRegionCode { get => _eiaRegionCode; set => SetProperty(ref _eiaRegionCode, value); }

    public ObservableCollection<Core.Models.Location> LocationSearchResults { get; } = new();
    public ObservableCollection<CultureOption> Currencies { get; } = new(SupportedCultures.All);
    public ObservableCollection<TemperatureUnitOption> TemperatureUnitsCollection { get; } = new(TemperatureUnits.All);
    public ObservableCollection<FuelTypeOption> FuelTypesCollection { get; } = new(FuelTypes.All);
    public ObservableCollection<MonthOption> MonthsCollection { get; } = new(MonthOptions.All);

    public string TankCapacityText { get => _tankCapacityText; set => SetProperty(ref _tankCapacityText, value); }
    public string LocationSearch { get => _locationSearch; set => SetProperty(ref _locationSearch, value); }
    public string LocationDisplay { get => _locationDisplay; set => SetProperty(ref _locationDisplay, value); }
    public bool IsSearchingLocation { get => _isSearchingLocation; set => SetProperty(ref _isSearchingLocation, value); }
    public bool IsFetchingWeather { get => _isFetchingWeather; set => SetProperty(ref _isFetchingWeather, value); }
    public string WeatherStatus { get => _weatherStatus; set => SetProperty(ref _weatherStatus, value); }
    public bool ReminderEnabled { get => _reminderEnabled; set => SetProperty(ref _reminderEnabled, value); }
    public string ThresholdGallonsText { get => _thresholdGallonsText; set => SetProperty(ref _thresholdGallonsText, value); }
    public string ThresholdDaysText { get => _thresholdDaysText; set => SetProperty(ref _thresholdDaysText, value); }
    public string DataFilePath { get => _dataFilePath; set => SetProperty(ref _dataFilePath, value); }
    public bool HasSearchResults => LocationSearchResults.Count > 0;
    public string EiaApiKey { get => _eiaApiKey; set => SetProperty(ref _eiaApiKey, value); }
    public string EiaKeyStatus { get => _eiaKeyStatus; set => SetProperty(ref _eiaKeyStatus, value); }

    // Sync properties
    public string SyncStatusText { get => _syncStatusText; set => SetProperty(ref _syncStatusText, value); }
    public bool IsSyncing { get => _isSyncing; set => SetProperty(ref _isSyncing, value); }
    public bool IsSignedIn { get => _isSignedIn; set { SetProperty(ref _isSignedIn, value); OnPropertyChanged(nameof(IsNotSignedIn)); } }
    public bool IsNotSignedIn => !_isSignedIn;
    public string SyncAccountText { get => _syncAccountText; set => SetProperty(ref _syncAccountText, value); }
    public string LastSyncedText { get => _lastSyncedText; set => SetProperty(ref _lastSyncedText, value); }
    public bool SyncConfigured => _syncService?.IsConfigured ?? false;

    public CultureOption SelectedCulture { get => _selectedCulture; set => SetProperty(ref _selectedCulture, value); }
    public TemperatureUnitOption SelectedTemperatureUnit { get => _selectedTemperatureUnit; set => SetProperty(ref _selectedTemperatureUnit, value); }
    public FuelTypeOption SelectedFuelType { get => _selectedFuelType; set => SetProperty(ref _selectedFuelType, value); }
    public MonthOption SelectedHeatingSeasonStartMonth { get => _selectedHeatingSeasonStartMonth; set => SetProperty(ref _selectedHeatingSeasonStartMonth, value); }
    public MonthOption SelectedHeatingSeasonEndMonth { get => _selectedHeatingSeasonEndMonth; set => SetProperty(ref _selectedHeatingSeasonEndMonth, value); }

    public Core.Models.Location? SelectedSearchResult
    {
        get => _selectedSearchResult;
        set
        {
            SetProperty(ref _selectedSearchResult, value);
            if (value != null)
                _ = SetLocationAsync(value);
        }
    }

    public string FetchWeatherButtonText => IsFetchingWeather ? "Fetching..." : "Fetch Weather Data";
    public string SearchButtonText => IsSearchingLocation ? "Searching..." : "Search";

    public ICommand SaveCommand { get; }
    public ICommand SearchLocationCommand { get; }
    public ICommand FetchWeatherCommand { get; }
    public ICommand ConnectDriveCommand { get; }
    public ICommand DisconnectDriveCommand { get; }
    public ICommand SyncNowCommand { get; }

    public SettingsViewModel(IDataService dataService, IWeatherService weatherService, ISyncService? syncService = null)
    {
        _dataService = dataService;
        _weatherService = weatherService;
        _syncService = syncService;

        SaveCommand = new Command(async () => await SaveAsync());
        SearchLocationCommand = new Command(async () => await SearchLocationAsync());
        FetchWeatherCommand = new Command(async () => await FetchWeatherAsync());
        ConnectDriveCommand = new Command(async () => await ConnectDriveAsync());
        DisconnectDriveCommand = new Command(async () => await DisconnectDriveAsync());
        SyncNowCommand = new Command(async () => await SyncNowAsync());

        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        TankCapacityText = (await _dataService.GetTankCapacityAsync()).ToString("F0");
        DataFilePath = _dataService.GetDataFilePath();

        var regionalSettings = await _dataService.GetRegionalSettingsAsync();
        SelectedCulture = SupportedCultures.GetByCode(regionalSettings.CultureCode);
        SelectedTemperatureUnit = TemperatureUnits.GetByCode(regionalSettings.TemperatureUnit);
        SelectedFuelType = FuelTypes.GetByCode(regionalSettings.FuelTypeCode);
        SelectedHeatingSeasonStartMonth = MonthOptions.GetByNumber(regionalSettings.HeatingSeasonStartMonth);
        SelectedHeatingSeasonEndMonth = MonthOptions.GetByNumber(regionalSettings.HeatingSeasonEndMonth);

        var location = await _dataService.GetLocationAsync();
        LocationDisplay = location.IsSet ? location.DisplayName : "Not set";

        var weather = await _dataService.GetWeatherHistoryAsync();
        if (weather.Count > 0)
        {
            var minDate = weather.Min(w => w.Date);
            var maxDate = weather.Max(w => w.Date);
            WeatherStatus = $"{weather.Count} days ({minDate:MMM d, yyyy} – {maxDate:MMM d, yyyy})";
        }
        else
        {
            WeatherStatus = "No weather data";
        }

        var reminderSettings = await _dataService.GetReminderSettingsAsync();
        ReminderEnabled = reminderSettings.IsEnabled;
        ThresholdGallonsText = reminderSettings.ThresholdGallons.ToString("F0");
        ThresholdDaysText = reminderSettings.ThresholdDays?.ToString() ?? string.Empty;

        // Load EIA API key + region
        EiaApiKey = Preferences.Get("eia_api_key", string.Empty);
        EiaKeyStatus = string.IsNullOrWhiteSpace(EiaApiKey) ? "Not configured" : "Key saved";
        EiaRegionCode = regionalSettings.EiaRegionCode;
        _selectedEiaRegion = EiaRegions.FirstOrDefault(r => r.Code == EiaRegionCode) ?? EiaRegions[0];
        OnPropertyChanged(nameof(SelectedEiaRegion));

        RefreshSyncStatus();
    }

    private void RefreshSyncStatus()
    {
        if (_syncService is null || !_syncService.IsConfigured)
        {
            IsSignedIn = false;
            SyncStatusText = "Not configured";
            SyncAccountText = string.Empty;
            LastSyncedText = string.Empty;
            return;
        }

        IsSignedIn = _syncService.IsSignedIn;
        if (_syncService.IsSignedIn)
        {
            SyncAccountText = _syncService.AccountEmail ?? "Connected";
            SyncStatusText = $"Connected as {SyncAccountText}";
            LastSyncedText = _syncService.LastSyncAt.HasValue
                ? $"Last synced: {FormatRelativeTime(_syncService.LastSyncAt.Value)}"
                : "Never synced";
        }
        else
        {
            SyncStatusText = "Not connected";
            SyncAccountText = string.Empty;
            LastSyncedText = string.Empty;
        }
    }

    private static string FormatRelativeTime(DateTime utcTime)
    {
        var elapsed = DateTime.UtcNow - utcTime;
        if (elapsed.TotalMinutes < 1) return "just now";
        if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes} minutes ago";
        if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours} hours ago";
        return utcTime.ToLocalTime().ToString("MMM d, yyyy h:mm tt");
    }

    private async Task ConnectDriveAsync()
    {
        if (_syncService is null) return;
        IsSyncing = true;
        try
        {
            var success = await _syncService.SignInAsync();
            if (success)
            {
                RefreshSyncStatus();
                await Shell.Current.DisplayAlert("Google Drive", "Connected successfully.", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Google Drive", "Sign-in was cancelled or failed.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Sign-in failed: {ex.Message}", "OK");
        }
        finally
        {
            IsSyncing = false;
        }
    }

    private async Task DisconnectDriveAsync()
    {
        if (_syncService is null) return;
        var confirmed = await Shell.Current.DisplayAlert(
            "Disconnect", "Disconnect from Google Drive? Local data will not be affected.", "Disconnect", "Cancel");
        if (!confirmed) return;

        await _syncService.SignOutAsync();
        RefreshSyncStatus();
    }

    private async Task SyncNowAsync()
    {
        if (_syncService is null) return;
        IsSyncing = true;
        try
        {
            var localData = await _dataService.LoadAsync();
            var result = await _syncService.SyncOnStartupAsync(localData);
            if (result.Status == SyncStatus.Success)
            {
                await _dataService.SaveAsync(result.MergedData);
                _dataService.InvalidateCache();
                RefreshSyncStatus();
                await Shell.Current.DisplayAlert("Sync", "Sync completed successfully.", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Sync", $"Sync failed: {result.Status}", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Sync failed: {ex.Message}", "OK");
        }
        finally
        {
            IsSyncing = false;
        }
    }

    private async Task SaveAsync()
    {
        if (!decimal.TryParse(TankCapacityText, out var capacity) || capacity <= 0)
        {
            await Shell.Current.DisplayAlert("Validation", "Tank capacity must be greater than zero.", "OK");
            return;
        }

        if (!decimal.TryParse(ThresholdGallonsText, out var thresholdGallons) || thresholdGallons < 0)
        {
            await Shell.Current.DisplayAlert("Validation", "Threshold gallons must be zero or greater.", "OK");
            return;
        }

        int? thresholdDays = null;
        if (!string.IsNullOrWhiteSpace(ThresholdDaysText) && int.TryParse(ThresholdDaysText, out var days))
            thresholdDays = days;

        await _dataService.SetTankCapacityAsync(capacity);

        await _dataService.SetRegionalSettingsAsync(new RegionalSettings
        {
            CultureCode = SelectedCulture.Code,
            TemperatureUnit = SelectedTemperatureUnit.Code,
            FuelTypeCode = SelectedFuelType.Code,
            HeatingSeasonStartMonth = SelectedHeatingSeasonStartMonth.Number,
            HeatingSeasonEndMonth = SelectedHeatingSeasonEndMonth.Number,
            EiaRegionCode = EiaRegionCode,
        });

        await _dataService.SetReminderSettingsAsync(new ReminderSettings
        {
            IsEnabled = ReminderEnabled,
            ThresholdGallons = thresholdGallons,
            ThresholdDays = thresholdDays
        });

        // Save EIA API key
        if (string.IsNullOrWhiteSpace(EiaApiKey))
            Preferences.Remove("eia_api_key");
        else
            Preferences.Set("eia_api_key", EiaApiKey.Trim());
        EiaKeyStatus = string.IsNullOrWhiteSpace(EiaApiKey) ? "Not configured" : "Key saved";

        await Shell.Current.DisplayAlert("Settings", "Settings saved.", "OK");
    }

    private async Task SearchLocationAsync()
    {
        if (string.IsNullOrWhiteSpace(LocationSearch)) return;

        IsSearchingLocation = true;
        OnPropertyChanged(nameof(SearchButtonText));
        LocationSearchResults.Clear();

        try
        {
            var results = await _weatherService.SearchLocationsAsync(LocationSearch.Trim());
            if (results.Count > 0)
            {
                foreach (var loc in results)
                    LocationSearchResults.Add(loc);
                OnPropertyChanged(nameof(HasSearchResults));
            }
            else
            {
                await Shell.Current.DisplayAlert("No Results", "No locations found. Try a different search term.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error searching: {ex.Message}", "OK");
        }
        finally
        {
            IsSearchingLocation = false;
            OnPropertyChanged(nameof(SearchButtonText));
        }
    }

    private async Task SetLocationAsync(Core.Models.Location location)
    {
        await _dataService.SetLocationAsync(location);
        LocationDisplay = location.DisplayName;
        LocationSearchResults.Clear();
        OnPropertyChanged(nameof(HasSearchResults));

        // Auto-detect EIA region from coordinates if user hasn't manually chosen one
        if (string.IsNullOrEmpty(EiaRegionCode))
        {
            EiaRegionCode = Core.Models.EiaRegionMapper.FromCoordinates(location.Latitude, location.Longitude);
        }

        await Shell.Current.DisplayAlert("Location Set", $"Location set to {location.DisplayName}", "OK");
    }

    private async Task FetchWeatherAsync()
    {
        var location = await _dataService.GetLocationAsync();
        if (!location.IsSet)
        {
            await Shell.Current.DisplayAlert("Location Required", "Please set a location first.", "OK");
            return;
        }

        var deliveries = await _dataService.GetDeliveriesAsync();
        if (deliveries.Count == 0)
        {
            await Shell.Current.DisplayAlert("No Deliveries", "Add deliveries first to fetch relevant weather data.", "OK");
            return;
        }

        IsFetchingWeather = true;
        OnPropertyChanged(nameof(FetchWeatherButtonText));
        WeatherStatus = "Fetching weather data...";

        try
        {
            var startDate = deliveries.Min(d => d.Date).AddDays(-1);
            var endDate = DateTime.Today;

            var weatherData = await _weatherService.GetHistoricalWeatherAsync(
                location.Latitude, location.Longitude, startDate, endDate);

            if (weatherData.Count > 0)
            {
                await _dataService.AddWeatherDataAsync(weatherData);
                WeatherStatus = $"{weatherData.Count} days fetched ({startDate:MMM d, yyyy} – {endDate:MMM d, yyyy})";
                await Shell.Current.DisplayAlert("Success", $"Fetched {weatherData.Count} days of weather data.", "OK");
            }
            else
            {
                WeatherStatus = "No data returned from API";
                await Shell.Current.DisplayAlert("No Data", "No weather data returned. Try again later.", "OK");
            }
        }
        catch (Exception ex)
        {
            WeatherStatus = "Error fetching weather";
            await Shell.Current.DisplayAlert("Error", $"Error: {ex.Message}", "OK");
        }
        finally
        {
            IsFetchingWeather = false;
            OnPropertyChanged(nameof(FetchWeatherButtonText));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}