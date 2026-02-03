using HeatingOilTracker.Models;
using HeatingOilTracker.Services;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System.Collections.ObjectModel;
using System.Windows;

namespace HeatingOilTracker.ViewModels;

public class ReportsViewModel : BindableBase, INavigationAware
{
    private readonly IReportService _reportService;

    // Available years
    private ObservableCollection<int> _availableYears = new();
    private int _selectedYear;

    // Current year summary
    private YearlySummary? _currentSummary;
    private SeasonalBreakdown? _currentBreakdown;

    // All yearly summaries for comparison
    private ObservableCollection<YearlySummary> _allSummaries = new();

    // State
    private bool _hasData;

    public ObservableCollection<int> AvailableYears
    {
        get => _availableYears;
        set => SetProperty(ref _availableYears, value);
    }

    public int SelectedYear
    {
        get => _selectedYear;
        set
        {
            if (SetProperty(ref _selectedYear, value) && value > 0)
            {
                _ = LoadYearDataAsync(value);
            }
        }
    }

    public YearlySummary? CurrentSummary
    {
        get => _currentSummary;
        set
        {
            SetProperty(ref _currentSummary, value);
            RaisePropertyChanged(nameof(TotalCost));
            RaisePropertyChanged(nameof(TotalGallons));
            RaisePropertyChanged(nameof(AvgPricePerGallon));
            RaisePropertyChanged(nameof(DeliveryCount));
            RaisePropertyChanged(nameof(TotalHDD));
            RaisePropertyChanged(nameof(CostPerHDD));
            RaisePropertyChanged(nameof(AvgKFactor));
            RaisePropertyChanged(nameof(TotalCO2Lbs));
            RaisePropertyChanged(nameof(TotalCO2MetricTons));
            RaisePropertyChanged(nameof(CO2LbsPerHDD));
            RaisePropertyChanged(nameof(OffsetCostRange));
            RaisePropertyChanged(nameof(OffsetCostLow));
            RaisePropertyChanged(nameof(OffsetCostHigh));
        }
    }

    public SeasonalBreakdown? CurrentBreakdown
    {
        get => _currentBreakdown;
        set
        {
            SetProperty(ref _currentBreakdown, value);
            RaisePropertyChanged(nameof(HeatingSeasonCostPercent));
            RaisePropertyChanged(nameof(OffSeasonCostPercent));
            RaisePropertyChanged(nameof(HeatingSeasonCO2Percent));
            RaisePropertyChanged(nameof(OffSeasonCO2Percent));
        }
    }

    public ObservableCollection<YearlySummary> AllSummaries
    {
        get => _allSummaries;
        set => SetProperty(ref _allSummaries, value);
    }

    public bool HasData
    {
        get => _hasData;
        set => SetProperty(ref _hasData, value);
    }

    // Display properties from CurrentSummary
    public string TotalCost => CurrentSummary != null ? CurrentSummary.TotalCost.ToString("C2") : "--";
    public string TotalGallons => CurrentSummary != null ? $"{CurrentSummary.TotalGallons:F1}" : "--";
    public string AvgPricePerGallon => CurrentSummary != null ? $"{CurrentSummary.AvgPricePerGallon:F3}" : "--";
    public string DeliveryCount => CurrentSummary != null ? CurrentSummary.DeliveryCount.ToString() : "--";
    public string TotalHDD => CurrentSummary?.TotalHDD.HasValue == true ? $"{CurrentSummary.TotalHDD.Value:F0}" : "--";
    public string CostPerHDD => CurrentSummary?.CostPerHDD.HasValue == true ? $"{CurrentSummary.CostPerHDD.Value:F2}" : "--";
    public string AvgKFactor => CurrentSummary?.AvgKFactor.HasValue == true ? $"{CurrentSummary.AvgKFactor.Value:F3}" : "--";

    // Display properties from CurrentBreakdown
    public string HeatingSeasonCostPercent => CurrentBreakdown != null ? $"{CurrentBreakdown.HeatingSeasonCostPercent:F0}%" : "--";
    public string OffSeasonCostPercent => CurrentBreakdown != null ? $"{CurrentBreakdown.OffSeasonCostPercent:F0}%" : "--";

    // Carbon footprint display properties
    public string TotalCO2Lbs => CurrentSummary != null ? $"{CurrentSummary.TotalCO2Lbs:N0}" : "--";
    public string TotalCO2MetricTons => CurrentSummary != null ? $"{CurrentSummary.TotalCO2MetricTons:F2}" : "--";
    public string CO2LbsPerHDD => CurrentSummary?.CO2LbsPerHDD.HasValue == true ? $"{CurrentSummary.CO2LbsPerHDD.Value:F2}" : "--";
    public string HeatingSeasonCO2Percent => CurrentBreakdown != null ? $"{CurrentBreakdown.HeatingSeasonCO2Percent:F0}%" : "--";
    public string OffSeasonCO2Percent => CurrentBreakdown != null ? $"{CurrentBreakdown.OffSeasonCO2Percent:F0}%" : "--";

    // Carbon offset cost estimates
    public string OffsetCostLow => CurrentSummary != null ? CurrentSummary.OffsetCostLow.ToString("C0") : "--";
    public string OffsetCostHigh => CurrentSummary != null ? CurrentSummary.OffsetCostHigh.ToString("C0") : "--";
    public string OffsetCostRange => CurrentSummary != null
        ? $"{CurrentSummary.OffsetCostLow:C0} – {CurrentSummary.OffsetCostHigh:C0}"
        : "--";

    public DelegateCommand RefreshCommand { get; }

    public ReportsViewModel(IReportService reportService)
    {
        _reportService = reportService;

        RefreshCommand = new DelegateCommand(async () => await LoadReportsAsync());

        _ = LoadReportsAsync();
    }

    private async Task LoadReportsAsync()
    {
        try
        {
            var years = await _reportService.GetAvailableYearsAsync();
            AvailableYears = new ObservableCollection<int>(years);

            if (years.Count > 0)
            {
                HasData = true;

                // Load all summaries for comparison table
                var summaries = await _reportService.GetAllYearlySummariesAsync();
                AllSummaries = new ObservableCollection<YearlySummary>(summaries);

                // Select most recent year
                SelectedYear = years.First();
            }
            else
            {
                HasData = false;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading reports: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadYearDataAsync(int year)
    {
        try
        {
            CurrentSummary = await _reportService.GetYearlySummaryAsync(year);
            CurrentBreakdown = await _reportService.GetSeasonalBreakdownAsync(year);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading year data: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void OnNavigatedTo(NavigationContext navigationContext) => _ = LoadReportsAsync();
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }
}
