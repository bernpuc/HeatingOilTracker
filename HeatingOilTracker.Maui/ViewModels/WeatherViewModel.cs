using HeatingOilTracker.Core.Interfaces;
using HeatingOilTracker.Core.Models;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace HeatingOilTracker.Maui.ViewModels;

public class WeatherViewModel : INotifyPropertyChanged
{
    private readonly IDataService _dataService;

    private static readonly SolidColorPaint CrosshairPaint = new(SKColor.Parse("#94a3b8"))
    {
        StrokeThickness = 1,
        PathEffect = new DashEffect([4, 4])
    };

    private List<DailyWeather> _allWeatherData = [];
    private string _temperatureUnit = "F";

    private bool _hasData;
    private string _selectedRange = "1M";
    private string _dataPointsInfo = "";
    private string _aggregationInfo = "";

    private ObservableCollection<ISeries> _temperatureSeries = new();
    private Axis[] _temperatureXAxes = [];
    private Axis[] _temperatureYAxes = [];
    private bool _isRefreshing;

    public bool HasData { get => _hasData; set => SetProperty(ref _hasData, value); }
    public string SelectedRange { get => _selectedRange; set => SetProperty(ref _selectedRange, value); }
    public string DataPointsInfo { get => _dataPointsInfo; set => SetProperty(ref _dataPointsInfo, value); }
    public string AggregationInfo { get => _aggregationInfo; set => SetProperty(ref _aggregationInfo, value); }
    public ObservableCollection<ISeries> TemperatureSeries { get => _temperatureSeries; set => SetProperty(ref _temperatureSeries, value); }
    public Axis[] TemperatureXAxes { get => _temperatureXAxes; set => SetProperty(ref _temperatureXAxes, value); }
    public Axis[] TemperatureYAxes { get => _temperatureYAxes; set => SetProperty(ref _temperatureYAxes, value); }

    public string TemperatureUnitLabel => _temperatureUnit == "C" ? "°C" : "°F";

    public ICommand Select1WCommand { get; }
    public ICommand Select1MCommand { get; }
    public ICommand Select3MCommand { get; }
    public ICommand Select1YCommand { get; }
    public ICommand SelectAllCommand { get; }
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    public ICommand RefreshCommand => new Command(async () =>
    {
        IsRefreshing = true;
        await LoadWeatherAsync();  // or whatever the page's load method is
        IsRefreshing = false;
    });

    public WeatherViewModel(IDataService dataService)
    {
        _dataService = dataService;

        Select1WCommand = new Command(() => ApplyPresetRange("1W"));
        Select1MCommand = new Command(() => ApplyPresetRange("1M"));
        Select3MCommand = new Command(() => ApplyPresetRange("3M"));
        Select1YCommand = new Command(() => ApplyPresetRange("1Y"));
        SelectAllCommand = new Command(() => ApplyPresetRange("All"));

        InitializeAxes();
    }

    private void InitializeAxes()
    {
        TemperatureXAxes =
        [
            new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString("MMM d"))
            {
                //Name = "Date",
                //NamePaint = new SolidColorPaint(SKColors.Gray),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                CrosshairPaint = CrosshairPaint,
                CrosshairSnapEnabled = true,
                TextSize=10
            }
        ];
        TemperatureYAxes =
        [
            new Axis
            {
                //Name = "Temperature (°F)",
                //NamePaint = new SolidColorPaint(SKColors.Gray),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                Labeler = value => $"{value:F0}°",
                TextSize=10
            }
        ];
    }

    public async Task LoadWeatherAsync()
    {
        _allWeatherData = await _dataService.GetWeatherHistoryAsync();
        var regionalSettings = await _dataService.GetRegionalSettingsAsync();
        _temperatureUnit = regionalSettings.TemperatureUnit;

        HasData = _allWeatherData.Count > 0;

        if (HasData)
        {
            TemperatureYAxes =
            [
                new Axis
                {
                    //Name = $"Temperature ({TemperatureUnitLabel})",
                    //NamePaint = new SolidColorPaint(SKColors.Gray),
                    LabelsPaint = new SolidColorPaint(SKColors.Gray),
                    Labeler = value => $"{value:F0}°",
                    TextSize=10
                }
            ];
            ApplyPresetRange(_selectedRange);
        }
        else
        {
            TemperatureSeries = new ObservableCollection<ISeries>();
            DataPointsInfo = "No weather data available.";
            AggregationInfo = "";
        }
    }

    private void ApplyPresetRange(string range)
    {
        SelectedRange = range;

        var end = DateTime.Today;
        var start = range switch
        {
            "1W" => end.AddDays(-7),
            "1M" => end.AddMonths(-1),
            "3M" => end.AddMonths(-3),
            "1Y" => end.AddYears(-1),
            "All" => _allWeatherData.Count > 0 ? _allWeatherData.Min(w => w.Date) : end.AddMonths(-1),
            _ => end.AddMonths(-1)
        };

        UpdateChart(start, end);
    }

    private void UpdateChart(DateTime startDate, DateTime endDate)
    {
        if (_allWeatherData.Count == 0) return;

        var filteredData = _allWeatherData
            .Where(w => w.Date >= startDate && w.Date <= endDate)
            .OrderBy(w => w.Date)
            .ToList();

        if (filteredData.Count == 0)
        {
            TemperatureSeries = new ObservableCollection<ISeries>();
            DataPointsInfo = "No data in selected range.";
            AggregationInfo = "";
            return;
        }

        var daySpan = (endDate - startDate).TotalDays;
        var (aggregatedData, aggregationType, xAxisFormat) = AggregateData(filteredData, daySpan);

        TemperatureXAxes =
        [
            new DateTimeAxis(GetAxisUnit(aggregationType), date => date.ToString(xAxisFormat))
            {
                //Name = "Date",
                //NamePaint = new SolidColorPaint(SKColors.Gray),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                CrosshairPaint = CrosshairPaint,
                CrosshairSnapEnabled = true,
                TextSize=10
            }
        ];

        var avgPoints = new List<DateTimePoint>();
        var highPoints = new List<DateTimePoint>();
        var lowPoints = new List<DateTimePoint>();

        foreach (var point in aggregatedData)
        {
            var high = _temperatureUnit == "C" ? (point.HighTempF - 32m) * 5m / 9m : point.HighTempF;
            var low = _temperatureUnit == "C" ? (point.LowTempF - 32m) * 5m / 9m : point.LowTempF;
            var avg = (high + low) / 2m;

            highPoints.Add(new DateTimePoint(point.Date, (double)high));
            avgPoints.Add(new DateTimePoint(point.Date, (double)avg));
            lowPoints.Add(new DateTimePoint(point.Date, (double)low));
        }

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

        DataPointsInfo = $"{filteredData.Count} days ({filteredData.Min(w => w.Date):MMM d, yyyy} – {filteredData.Max(w => w.Date):MMM d, yyyy})";
        AggregationInfo = aggregationType switch
        {
            "daily" => $"Daily temperatures ({aggregatedData.Count} points)",
            "weekly" => $"Weekly averages ({aggregatedData.Count} points)",
            "monthly" => $"Monthly averages ({aggregatedData.Count} points)",
            _ => ""
        };
    }

    private static (List<DailyWeather> Data, string Type, string Format) AggregateData(List<DailyWeather> data, double daySpan)
    {
        if (daySpan <= 30)
            return (data, "daily", "MMM d");

        if (daySpan <= 180)
        {
            var weekly = data
                .GroupBy(w => new { w.Date.Year, Week = GetWeekOfYear(w.Date) })
                .Select(g => new DailyWeather
                {
                    Date = g.Min(w => w.Date),
                    HighTempF = g.Average(w => w.HighTempF),
                    LowTempF = g.Average(w => w.LowTempF)
                })
                .OrderBy(w => w.Date)
                .ToList();
            return (weekly, "weekly", "MMM d");
        }

        var monthly = data
            .GroupBy(w => new { w.Date.Year, w.Date.Month })
            .Select(g => new DailyWeather
            {
                Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                HighTempF = g.Average(w => w.HighTempF),
                LowTempF = g.Average(w => w.LowTempF)
            })
            .OrderBy(w => w.Date)
            .ToList();
        return (monthly, "monthly", "MMM yyyy");
    }

    private static int GetWeekOfYear(DateTime date)
    {
        var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
        return cal.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
    }

    private static TimeSpan GetAxisUnit(string aggregationType) => aggregationType switch
    {
        "weekly" => TimeSpan.FromDays(7),
        "monthly" => TimeSpan.FromDays(30),
        _ => TimeSpan.FromDays(1)
    };

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