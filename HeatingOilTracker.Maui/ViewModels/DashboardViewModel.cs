using HeatingOilTracker.Core.Interfaces;
using HeatingOilTracker.Core.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace HeatingOilTracker.Maui.ViewModels;

public class DashboardViewModel : INotifyPropertyChanged
{
    private readonly IDataService _dataService;
    private readonly ITankEstimatorService _tankEstimatorService;

    // Tank Status
    private decimal _estimatedGallons;
    private decimal _tankCapacity;
    private decimal _percentFull;
    private int _daysSinceLastDelivery;
    private DateTime? _lastDeliveryDate;
    private decimal _burnRate;
    private int? _daysRemaining;
    private decimal? _averageKFactor;
    private bool _hasData;
    private bool _showAlert;
    private string _alertMessage = string.Empty;
    private string _predictedRefillDateText = "--";
    private ObservableCollection<OilDelivery> _recentDeliveries = new();
    private bool _isRefreshing;

    public decimal EstimatedGallons { get => _estimatedGallons; set => SetProperty(ref _estimatedGallons, value); }
    public decimal TankCapacity { get => _tankCapacity; set => SetProperty(ref _tankCapacity, value); }
    public decimal PercentFull { get => _percentFull; set => SetProperty(ref _percentFull, value); }
    public int DaysSinceLastDelivery { get => _daysSinceLastDelivery; set => SetProperty(ref _daysSinceLastDelivery, value); }
    public DateTime? LastDeliveryDate { get => _lastDeliveryDate; set => SetProperty(ref _lastDeliveryDate, value); }
    public decimal BurnRate { get => _burnRate; set => SetProperty(ref _burnRate, value); }
    public int? DaysRemaining { get => _daysRemaining; set => SetProperty(ref _daysRemaining, value); }
    public decimal? AverageKFactor { get => _averageKFactor; set => SetProperty(ref _averageKFactor, value); }
    public bool HasData { get => _hasData; set => SetProperty(ref _hasData, value); }
    public bool ShowAlert { get => _showAlert; set => SetProperty(ref _showAlert, value); }
    public string AlertMessage { get => _alertMessage; set => SetProperty(ref _alertMessage, value); }
    public string PredictedRefillDateText { get => _predictedRefillDateText; set => SetProperty(ref _predictedRefillDateText, value); }
    public ObservableCollection<OilDelivery> RecentDeliveries { get => _recentDeliveries; set => SetProperty(ref _recentDeliveries, value); }

    // Computed display strings
    public string BurnRateDisplay => BurnRate > 0 ? $"{BurnRate:F1} gal/day" : "--";
    public string KFactorDisplay => AverageKFactor.HasValue ? $"{AverageKFactor.Value:F1} HDD/gal" : "--";
    public string DaysRemainingDisplay => DaysRemaining.HasValue ? $"~{DaysRemaining.Value} days" : "--";
    public string LastDeliveryDateDisplay => LastDeliveryDate.HasValue ? LastDeliveryDate.Value.ToString("MMM d, yyyy") : "--";
    public double TankFillHeight => (double)PercentFull / 100.0 * 120.0;
    public string TankEighthsDisplay
    {
        get
        {
            var eighths = (int)Math.Round((double)PercentFull / 100.0 * 8.0, MidpointRounding.AwayFromZero);
            eighths = Math.Clamp(eighths, 0, 8);
            return $"{eighths}/8";
        }
    }
    public bool IsRefreshing { get => _isRefreshing; set => SetProperty(ref _isRefreshing, value); }

    public ICommand RefreshCommand => new Command(async () =>
    {
        IsRefreshing = true;
        await LoadDashboardAsync();  // or whatever the page's load method is
        IsRefreshing = false;
    });

    public DashboardViewModel(IDataService dataService, ITankEstimatorService tankEstimatorService)
    {
        _dataService = dataService;
        _tankEstimatorService = tankEstimatorService;
        _ = LoadDashboardAsync();
    }

    public async Task LoadDashboardAsync()
    {
        try
        {
            var status = await _tankEstimatorService.GetCurrentStatusAsync();

            EstimatedGallons = status.EstimatedGallons;
            TankCapacity = status.TankCapacity;
            PercentFull = status.PercentFull;
            DaysSinceLastDelivery = status.DaysSinceLastDelivery;
            LastDeliveryDate = status.LastDeliveryDate;
            BurnRate = status.EstimatedBurnRate;
            DaysRemaining = status.EstimatedDaysRemaining;
            AverageKFactor = status.AverageKFactor;

            OnPropertyChanged(nameof(BurnRateDisplay));
            OnPropertyChanged(nameof(KFactorDisplay));
            OnPropertyChanged(nameof(DaysRemainingDisplay));
            OnPropertyChanged(nameof(LastDeliveryDateDisplay));
            OnPropertyChanged(nameof(TankFillHeight));
            OnPropertyChanged(nameof(TankEighthsDisplay));

            var reminderSettings = await _dataService.GetReminderSettingsAsync();

            if (reminderSettings.IsEnabled && EstimatedGallons <= reminderSettings.ThresholdGallons)
            {
                ShowAlert = true;
                AlertMessage = $"Tank level is low! Estimated {EstimatedGallons:F0} gallons remaining (threshold: {reminderSettings.ThresholdGallons:F0} gallons)";
            }
            else if (reminderSettings.IsEnabled && reminderSettings.ThresholdDays.HasValue
                     && DaysRemaining.HasValue && DaysRemaining.Value <= reminderSettings.ThresholdDays.Value)
            {
                ShowAlert = true;
                AlertMessage = $"Running low! Only ~{DaysRemaining.Value} days of oil remaining";
            }
            else
            {
                ShowAlert = false;
            }

            var predictedDate = await _tankEstimatorService.PredictRefillDateAsync(reminderSettings.ThresholdGallons);
            PredictedRefillDateText = predictedDate.HasValue ? $"~{predictedDate.Value:MMM d}" : "--";

            var deliveries = await _dataService.GetDeliveriesAsync();
            RecentDeliveries = new ObservableCollection<OilDelivery>(
                deliveries.OrderByDescending(d => d.Date).Take(3));
            HasData = deliveries.Count > 0;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error loading dashboard: {ex.Message}", "OK");
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