using HeatingOilTracker.Core.Interfaces;
using HeatingOilTracker.Core.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace HeatingOilTracker.Maui.ViewModels;

public class ReportsViewModel : INotifyPropertyChanged
{
    private readonly IReportService _reportService;

    private ObservableCollection<int> _availableYears = new();
    private int _selectedYear;
    private YearlySummary? _currentSummary;
    private SeasonalBreakdown? _currentBreakdown;
    private ObservableCollection<YearlySummary> _allSummaries = new();
    private bool _hasData;

    public ObservableCollection<int> AvailableYears { get => _availableYears; set => SetProperty(ref _availableYears, value); }
    public ObservableCollection<YearlySummary> AllSummaries { get => _allSummaries; set => SetProperty(ref _allSummaries, value); }
    public bool HasData { get => _hasData; set => SetProperty(ref _hasData, value); }

    public int SelectedYear
    {
        get => _selectedYear;
        set
        {
            if (SetProperty(ref _selectedYear, value) && value > 0)
                _ = LoadYearDataAsync(value);
        }
    }

    public YearlySummary? CurrentSummary
    {
        get => _currentSummary;
        set
        {
            SetProperty(ref _currentSummary, value);
            OnPropertyChanged(nameof(TotalCost));
            OnPropertyChanged(nameof(TotalGallons));
            OnPropertyChanged(nameof(AvgPricePerGallon));
            OnPropertyChanged(nameof(DeliveryCount));
            OnPropertyChanged(nameof(TotalHDD));
            OnPropertyChanged(nameof(CostPerHDD));
            OnPropertyChanged(nameof(AvgKFactor));
            OnPropertyChanged(nameof(TotalCO2Lbs));
            OnPropertyChanged(nameof(TotalCO2MetricTons));
            OnPropertyChanged(nameof(CO2LbsPerHDD));
            OnPropertyChanged(nameof(OffsetCostRange));
        }
    }

    public SeasonalBreakdown? CurrentBreakdown
    {
        get => _currentBreakdown;
        set
        {
            SetProperty(ref _currentBreakdown, value);
            OnPropertyChanged(nameof(HeatingSeasonCostPercent));
            OnPropertyChanged(nameof(OffSeasonCostPercent));
            OnPropertyChanged(nameof(HeatingSeasonCO2Percent));
            OnPropertyChanged(nameof(OffSeasonCO2Percent));
        }
    }

    // Summary display properties
    public string TotalCost => CurrentSummary != null ? CurrentSummary.TotalCost.ToString("C2") : "--";
    public string TotalGallons => CurrentSummary != null ? $"{CurrentSummary.TotalGallons:F1}" : "--";
    public string AvgPricePerGallon => CurrentSummary != null ? $"{CurrentSummary.AvgPricePerGallon:F3}" : "--";
    public string DeliveryCount => CurrentSummary != null ? CurrentSummary.DeliveryCount.ToString() : "--";
    public string TotalHDD => CurrentSummary?.TotalHDD.HasValue == true ? $"{CurrentSummary.TotalHDD.Value:F0}" : "--";
    public string CostPerHDD => CurrentSummary?.CostPerHDD.HasValue == true ? $"{CurrentSummary.CostPerHDD.Value:F2}" : "--";
    public string AvgKFactor => CurrentSummary?.AvgKFactor.HasValue == true ? $"{CurrentSummary.AvgKFactor.Value:F3}" : "--";
    public string TotalCO2Lbs => CurrentSummary != null ? $"{CurrentSummary.TotalCO2Lbs:N0}" : "--";
    public string TotalCO2MetricTons => CurrentSummary != null ? $"{CurrentSummary.TotalCO2MetricTons:F2}" : "--";
    public string CO2LbsPerHDD => CurrentSummary?.CO2LbsPerHDD.HasValue == true ? $"{CurrentSummary.CO2LbsPerHDD.Value:F2}" : "--";
    public string OffsetCostRange => CurrentSummary != null ? $"{CurrentSummary.OffsetCostLow:C0} – {CurrentSummary.OffsetCostHigh:C0}" : "--";
    public string HeatingSeasonCostPercent => CurrentBreakdown != null ? $"{CurrentBreakdown.HeatingSeasonCostPercent:F0}%" : "--";
    public string OffSeasonCostPercent => CurrentBreakdown != null ? $"{CurrentBreakdown.OffSeasonCostPercent:F0}%" : "--";
    public string HeatingSeasonCO2Percent => CurrentBreakdown != null ? $"{CurrentBreakdown.HeatingSeasonCO2Percent:F0}%" : "--";
    public string OffSeasonCO2Percent => CurrentBreakdown != null ? $"{CurrentBreakdown.OffSeasonCO2Percent:F0}%" : "--";

    public ICommand RefreshCommand { get; }

    public ReportsViewModel(IReportService reportService)
    {
        _reportService = reportService;
        RefreshCommand = new Command(async () => await LoadReportsAsync());
        _ = LoadReportsAsync();
    }

    public async Task LoadReportsAsync()
    {
        try
        {
            var years = await _reportService.GetAvailableYearsAsync();
            AvailableYears = new ObservableCollection<int>(years);

            if (years.Count > 0)
            {
                HasData = true;
                var summaries = await _reportService.GetAllYearlySummariesAsync();
                AllSummaries = new ObservableCollection<YearlySummary>(summaries);
                SelectedYear = years.First();
            }
            else
            {
                HasData = false;
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error loading reports: {ex.Message}", "OK");
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
            await Shell.Current.DisplayAlert("Error", $"Error loading year: {ex.Message}", "OK");
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