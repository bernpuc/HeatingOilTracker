using HeatingOilTracker.Models;
using HeatingOilTracker.Services;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System.Collections.ObjectModel;
using System.Windows;

namespace HeatingOilTracker.ViewModels;

public class DashboardViewModel : BindableBase, INavigationAware
{
    private readonly IDataService _dataService;
    private readonly ITankEstimatorService _tankEstimatorService;
    private readonly IWeatherService _weatherService;
    private readonly IRegionManager _regionManager;

    // Tank Status
    private decimal _estimatedGallons;
    private decimal _tankCapacity;
    private decimal _percentFull;
    private int _daysSinceLastDelivery;
    private DateTime? _lastDeliveryDate;
    private decimal _burnRate;
    private int? _daysRemaining;
    private decimal? _averageKFactor;

    // Alert
    private bool _showAlert;
    private string _alertMessage = string.Empty;
    private decimal _thresholdGallons;

    // Recent deliveries
    private ObservableCollection<OilDelivery> _recentDeliveries = new();

    // Predicted refill
    private string _predictedRefillDateText = string.Empty;

    // Loading state
    private bool _hasData;

    public decimal EstimatedGallons
    {
        get => _estimatedGallons;
        set => SetProperty(ref _estimatedGallons, value);
    }

    public decimal TankCapacity
    {
        get => _tankCapacity;
        set => SetProperty(ref _tankCapacity, value);
    }

    public decimal PercentFull
    {
        get => _percentFull;
        set => SetProperty(ref _percentFull, value);
    }

    public int DaysSinceLastDelivery
    {
        get => _daysSinceLastDelivery;
        set => SetProperty(ref _daysSinceLastDelivery, value);
    }

    public DateTime? LastDeliveryDate
    {
        get => _lastDeliveryDate;
        set => SetProperty(ref _lastDeliveryDate, value);
    }

    public decimal BurnRate
    {
        get => _burnRate;
        set => SetProperty(ref _burnRate, value);
    }

    public int? DaysRemaining
    {
        get => _daysRemaining;
        set => SetProperty(ref _daysRemaining, value);
    }

    public decimal? AverageKFactor
    {
        get => _averageKFactor;
        set => SetProperty(ref _averageKFactor, value);
    }

    public bool ShowAlert
    {
        get => _showAlert;
        set => SetProperty(ref _showAlert, value);
    }

    public string AlertMessage
    {
        get => _alertMessage;
        set => SetProperty(ref _alertMessage, value);
    }

    public decimal ThresholdGallons
    {
        get => _thresholdGallons;
        set => SetProperty(ref _thresholdGallons, value);
    }

    public ObservableCollection<OilDelivery> RecentDeliveries
    {
        get => _recentDeliveries;
        set => SetProperty(ref _recentDeliveries, value);
    }

    public string PredictedRefillDateText
    {
        get => _predictedRefillDateText;
        set => SetProperty(ref _predictedRefillDateText, value);
    }

    public bool HasData
    {
        get => _hasData;
        set => SetProperty(ref _hasData, value);
    }

    public string BurnRateDisplay => BurnRate > 0 ? $"{BurnRate:F1} gal/day" : "--";
    public string KFactorDisplay => AverageKFactor.HasValue ? $"{AverageKFactor.Value:F1} HDD/gal" : "--";
    public string DaysRemainingDisplay => DaysRemaining.HasValue ? $"~{DaysRemaining.Value} days" : "--";
    public string LastDeliveryDateDisplay => LastDeliveryDate.HasValue ? LastDeliveryDate.Value.ToString("MMM d, yyyy") : "--";

    public DelegateCommand NavigateToDeliveriesCommand { get; }
    public DelegateCommand NavigateToSettingsCommand { get; }

    public DashboardViewModel(
        IDataService dataService,
        ITankEstimatorService tankEstimatorService,
        IWeatherService weatherService,
        IRegionManager regionManager)
    {
        _dataService = dataService;
        _tankEstimatorService = tankEstimatorService;
        _weatherService = weatherService;
        _regionManager = regionManager;

        NavigateToDeliveriesCommand = new DelegateCommand(() =>
            _regionManager.RequestNavigate("ContentRegion", "DeliveriesView"));
        NavigateToSettingsCommand = new DelegateCommand(() =>
            _regionManager.RequestNavigate("ContentRegion", "SettingsView"));

        _ = LoadDashboardAsync();
    }

    private async Task LoadDashboardAsync()
    {
        try
        {
            // Load tank status
            var status = await _tankEstimatorService.GetCurrentStatusAsync();

            EstimatedGallons = status.EstimatedGallons;
            TankCapacity = status.TankCapacity;
            PercentFull = status.PercentFull;
            DaysSinceLastDelivery = status.DaysSinceLastDelivery;
            LastDeliveryDate = status.LastDeliveryDate;
            BurnRate = status.EstimatedBurnRate;
            DaysRemaining = status.EstimatedDaysRemaining;
            AverageKFactor = status.AverageKFactor;

            // Notify display properties changed
            RaisePropertyChanged(nameof(BurnRateDisplay));
            RaisePropertyChanged(nameof(KFactorDisplay));
            RaisePropertyChanged(nameof(DaysRemainingDisplay));
            RaisePropertyChanged(nameof(LastDeliveryDateDisplay));

            // Load reminder settings and check alert
            var reminderSettings = await _dataService.GetReminderSettingsAsync();
            ThresholdGallons = reminderSettings.ThresholdGallons;

            if (reminderSettings.IsEnabled && EstimatedGallons <= reminderSettings.ThresholdGallons)
            {
                ShowAlert = true;
                AlertMessage = $"Tank level is low! Estimated {EstimatedGallons:F0} gallons remaining (threshold: {reminderSettings.ThresholdGallons:F0} gallons)";
            }
            else if (reminderSettings.IsEnabled && reminderSettings.ThresholdDays.HasValue && DaysRemaining.HasValue &&
                     DaysRemaining.Value <= reminderSettings.ThresholdDays.Value)
            {
                ShowAlert = true;
                AlertMessage = $"Running low! Only ~{DaysRemaining.Value} days of oil remaining";
            }
            else
            {
                ShowAlert = false;
            }

            // Calculate predicted refill date
            var predictedDate = await _tankEstimatorService.PredictRefillDateAsync(reminderSettings.ThresholdGallons);
            PredictedRefillDateText = predictedDate.HasValue
                ? $"~{predictedDate.Value:MMM d}"
                : "--";

            // Load recent deliveries (last 3)
            var deliveries = await _dataService.GetDeliveriesAsync();
            RecentDeliveries = new ObservableCollection<OilDelivery>(
                deliveries.OrderByDescending(d => d.Date).Take(3));

            HasData = deliveries.Count > 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading dashboard: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void OnNavigatedTo(NavigationContext navigationContext) => _ = LoadDashboardAsync();
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }
}
