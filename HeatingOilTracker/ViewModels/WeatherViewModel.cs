using HeatingOilTracker.Models;
using HeatingOilTracker.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using SkiaSharp;
using System.Collections.ObjectModel;

namespace HeatingOilTracker.ViewModels;

public class WeatherViewModel : BindableBase, INavigationAware
{
    private readonly IDataService _dataService;

    private static readonly SolidColorPaint CrosshairPaint = new(SKColor.Parse("#94a3b8"))
    {
        StrokeThickness = 1,
        PathEffect = new DashEffect([4, 4])
    };

    // Data
    private List<DailyWeather> _allWeatherData = [];
    private string _temperatureUnit = "F";

    // UI State
    private bool _hasData;
    private string _selectedRange = "1M";
    private DateTime _startDate = DateTime.Today.AddMonths(-1);
    private DateTime _endDate = DateTime.Today;
    private string _dataPointsInfo = "";
    private string _aggregationInfo = "";

    // Chart
    private ObservableCollection<ISeries> _temperatureSeries = new();
    private Axis[] _temperatureXAxes = [];
    private Axis[] _temperatureYAxes = [];

    public bool HasData
    {
        get => _hasData;
        set => SetProperty(ref _hasData, value);
    }

    public string SelectedRange
    {
        get => _selectedRange;
        set
        {
            if (SetProperty(ref _selectedRange, value))
            {
                ApplyPresetRange(value);
            }
        }
    }

    public DateTime StartDate
    {
        get => _startDate;
        set
        {
            if (SetProperty(ref _startDate, value))
            {
                SelectedRange = "Custom";
                UpdateChart();
            }
        }
    }

    public DateTime EndDate
    {
        get => _endDate;
        set
        {
            if (SetProperty(ref _endDate, value))
            {
                SelectedRange = "Custom";
                UpdateChart();
            }
        }
    }

    public string DataPointsInfo
    {
        get => _dataPointsInfo;
        set => SetProperty(ref _dataPointsInfo, value);
    }

    public string AggregationInfo
    {
        get => _aggregationInfo;
        set => SetProperty(ref _aggregationInfo, value);
    }

    public ObservableCollection<ISeries> TemperatureSeries
    {
        get => _temperatureSeries;
        set => SetProperty(ref _temperatureSeries, value);
    }

    public Axis[] TemperatureXAxes
    {
        get => _temperatureXAxes;
        set => SetProperty(ref _temperatureXAxes, value);
    }

    public Axis[] TemperatureYAxes
    {
        get => _temperatureYAxes;
        set => SetProperty(ref _temperatureYAxes, value);
    }

    public string TemperatureUnitLabel => _temperatureUnit == "C" ? "°C" : "°F";

    // Commands for preset ranges
    public DelegateCommand Select1WCommand { get; }
    public DelegateCommand Select1MCommand { get; }
    public DelegateCommand Select3MCommand { get; }
    public DelegateCommand Select1YCommand { get; }
    public DelegateCommand SelectAllCommand { get; }

    public WeatherViewModel(IDataService dataService)
    {
        _dataService = dataService;

        Select1WCommand = new DelegateCommand(() => SelectedRange = "1W");
        Select1MCommand = new DelegateCommand(() => SelectedRange = "1M");
        Select3MCommand = new DelegateCommand(() => SelectedRange = "3M");
        Select1YCommand = new DelegateCommand(() => SelectedRange = "1Y");
        SelectAllCommand = new DelegateCommand(() => SelectedRange = "All");

        InitializeAxes();
    }

    private void InitializeAxes()
    {
        TemperatureXAxes =
        [
            new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString("MMM d"))
            {
                Name = "Date",
                NamePaint = new SolidColorPaint(SKColors.Gray),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                CrosshairPaint = CrosshairPaint,
                CrosshairSnapEnabled = true
            }
        ];

        TemperatureYAxes =
        [
            new Axis
            {
                Name = "Temperature (°F)",
                NamePaint = new SolidColorPaint(SKColors.Gray),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                Labeler = value => $"{value:F0}°"
            }
        ];
    }

    private void ApplyPresetRange(string range)
    {
        var end = DateTime.Today;
        var start = range switch
        {
            "1W" => end.AddDays(-7),
            "1M" => end.AddMonths(-1),
            "3M" => end.AddMonths(-3),
            "1Y" => end.AddYears(-1),
            "All" => _allWeatherData.Count > 0 ? _allWeatherData.Min(w => w.Date) : end.AddMonths(-1),
            _ => _startDate
        };

        // Update without triggering the property setters that would reset to Custom
        _startDate = start;
        _endDate = end;
        RaisePropertyChanged(nameof(StartDate));
        RaisePropertyChanged(nameof(EndDate));

        UpdateChart();
    }

    private async Task LoadDataAsync()
    {
        _allWeatherData = await _dataService.GetWeatherHistoryAsync();
        var regionalSettings = await _dataService.GetRegionalSettingsAsync();
        _temperatureUnit = regionalSettings.TemperatureUnit;

        HasData = _allWeatherData.Count > 0;

        if (HasData)
        {
            // Update Y-axis label based on temperature unit
            TemperatureYAxes =
            [
                new Axis
                {
                    Name = $"Temperature ({TemperatureUnitLabel})",
                    NamePaint = new SolidColorPaint(SKColors.Gray),
                    LabelsPaint = new SolidColorPaint(SKColors.Gray),
                    Labeler = value => $"{value:F0}°"
                }
            ];

            // Apply default range
            ApplyPresetRange(_selectedRange);
        }
        else
        {
            TemperatureSeries = new ObservableCollection<ISeries>();
            DataPointsInfo = "No weather data available. Configure location in Settings and fetch weather data.";
            AggregationInfo = "";
        }
    }

    private void UpdateChart()
    {
        if (_allWeatherData.Count == 0) return;

        // Filter data to selected range
        var filteredData = _allWeatherData
            .Where(w => w.Date >= _startDate && w.Date <= _endDate)
            .OrderBy(w => w.Date)
            .ToList();

        if (filteredData.Count == 0)
        {
            TemperatureSeries = new ObservableCollection<ISeries>();
            DataPointsInfo = "No data in selected range.";
            AggregationInfo = "";
            return;
        }

        // Determine aggregation level based on date range
        var daySpan = (_endDate - _startDate).TotalDays;
        var (aggregatedData, aggregationType, xAxisFormat) = AggregateData(filteredData, daySpan);

        // Update X-axis format based on aggregation
        TemperatureXAxes =
        [
            new DateTimeAxis(GetAxisUnit(aggregationType), date => date.ToString(xAxisFormat))
            {
                Name = "Date",
                NamePaint = new SolidColorPaint(SKColors.Gray),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                CrosshairPaint = CrosshairPaint,
                CrosshairSnapEnabled = true
            }
        ];

        // Build chart points
        var avgPoints = new List<DateTimePoint>();
        var highPoints = new List<DateTimePoint>();
        var lowPoints = new List<DateTimePoint>();

        foreach (var point in aggregatedData)
        {
            var temp = _temperatureUnit == "C" ? point.AvgTempC : point.AvgTempF;
            var high = _temperatureUnit == "C" ? (point.HighTempF - 32m) * 5m / 9m : point.HighTempF;
            var low = _temperatureUnit == "C" ? (point.LowTempF - 32m) * 5m / 9m : point.LowTempF;

            avgPoints.Add(new DateTimePoint(point.Date, (double)temp));
            highPoints.Add(new DateTimePoint(point.Date, (double)high));
            lowPoints.Add(new DateTimePoint(point.Date, (double)low));
        }

        // Create series
        TemperatureSeries = new ObservableCollection<ISeries>
        {
            new LineSeries<DateTimePoint>
            {
                Values = highPoints,
                Name = "High",
                Fill = null,
                GeometrySize = 0,
                Stroke = new SolidColorPaint(SKColor.Parse("#ef4444")) { StrokeThickness = 1.5f },
                LineSmoothness = 0.3,
                YToolTipLabelFormatter = point => $"High: {point.Model?.Value:F1}{TemperatureUnitLabel}"
            },
            new LineSeries<DateTimePoint>
            {
                Values = avgPoints,
                Name = "Average",
                Fill = null,
                GeometrySize = daySpan <= 30 ? 5 : 0,
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#3b82f6")) { StrokeThickness = 2 },
                GeometryFill = new SolidColorPaint(SKColors.White),
                Stroke = new SolidColorPaint(SKColor.Parse("#3b82f6")) { StrokeThickness = 2.5f },
                LineSmoothness = 0.3,
                YToolTipLabelFormatter = point => $"Avg: {point.Model?.Value:F1}{TemperatureUnitLabel}"
            },
            new LineSeries<DateTimePoint>
            {
                Values = lowPoints,
                Name = "Low",
                Fill = null,
                GeometrySize = 0,
                Stroke = new SolidColorPaint(SKColor.Parse("#22c55e")) { StrokeThickness = 1.5f },
                LineSmoothness = 0.3,
                YToolTipLabelFormatter = point => $"Low: {point.Model?.Value:F1}{TemperatureUnitLabel}"
            }
        };

        // Update info labels
        DataPointsInfo = $"{filteredData.Count} days of data ({filteredData.Min(w => w.Date):MMM d, yyyy} - {filteredData.Max(w => w.Date):MMM d, yyyy})";
        AggregationInfo = aggregationType switch
        {
            "daily" => $"Showing daily temperatures ({aggregatedData.Count} points)",
            "weekly" => $"Showing weekly averages ({aggregatedData.Count} points)",
            "monthly" => $"Showing monthly averages ({aggregatedData.Count} points)",
            _ => ""
        };
    }

    private static (List<DailyWeather> Data, string Type, string Format) AggregateData(List<DailyWeather> data, double daySpan)
    {
        // ≤30 days: Daily
        if (daySpan <= 30)
        {
            return (data, "daily", "MMM d");
        }

        // 31-180 days: Weekly
        if (daySpan <= 180)
        {
            var weeklyData = data
                .GroupBy(w => new { w.Date.Year, Week = GetWeekOfYear(w.Date) })
                .Select(g => new DailyWeather
                {
                    Date = g.Min(w => w.Date),
                    HighTempF = g.Average(w => w.HighTempF),
                    LowTempF = g.Average(w => w.LowTempF)
                })
                .OrderBy(w => w.Date)
                .ToList();
            return (weeklyData, "weekly", "MMM d");
        }

        // >180 days: Monthly
        var monthlyData = data
            .GroupBy(w => new { w.Date.Year, w.Date.Month })
            .Select(g => new DailyWeather
            {
                Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                HighTempF = g.Average(w => w.HighTempF),
                LowTempF = g.Average(w => w.LowTempF)
            })
            .OrderBy(w => w.Date)
            .ToList();
        return (monthlyData, "monthly", "MMM yyyy");
    }

    private static int GetWeekOfYear(DateTime date)
    {
        var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
        return cal.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
    }

    private static TimeSpan GetAxisUnit(string aggregationType)
    {
        return aggregationType switch
        {
            "daily" => TimeSpan.FromDays(1),
            "weekly" => TimeSpan.FromDays(7),
            "monthly" => TimeSpan.FromDays(30),
            _ => TimeSpan.FromDays(1)
        };
    }

    public void OnNavigatedTo(NavigationContext navigationContext) => _ = LoadDataAsync();
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }
}
