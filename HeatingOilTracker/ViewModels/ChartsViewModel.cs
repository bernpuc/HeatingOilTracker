using HeatingOilTracker.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using SkiaSharp;
using System.Collections.ObjectModel;

namespace HeatingOilTracker.ViewModels;

public class ChartsViewModel : BindableBase, INavigationAware
{
    private readonly IDataService _dataService;
    private readonly IWeatherService _weatherService;

    private static readonly SolidColorPaint CrosshairPaint = new(SKColor.Parse("#94a3b8"))
    {
        StrokeThickness = 1,
        PathEffect = new DashEffect([4, 4])
    };

    private bool _hasData;
    private bool _hasKFactorData;
    private ObservableCollection<ISeries> _burnRateSeries = new();
    private ObservableCollection<ISeries> _priceSeries = new();
    private ObservableCollection<ISeries> _kFactorSeries = new();
    private Axis[] _burnRateXAxes = [];
    private Axis[] _burnRateYAxes = [];
    private Axis[] _priceXAxes = [];
    private Axis[] _priceYAxes = [];
    private Axis[] _kFactorXAxes = [];
    private Axis[] _kFactorYAxes = [];
    private TooltipFindingStrategy _tooltipFindingStrategy = TooltipFindingStrategy.CompareOnlyXTakeClosest;

    public bool HasData
    {
        get => _hasData;
        set => SetProperty(ref _hasData, value);
    }

    public bool HasKFactorData
    {
        get => _hasKFactorData;
        set => SetProperty(ref _hasKFactorData, value);
    }

    public ObservableCollection<ISeries> BurnRateSeries
    {
        get => _burnRateSeries;
        set => SetProperty(ref _burnRateSeries, value);
    }

    public ObservableCollection<ISeries> PriceSeries
    {
        get => _priceSeries;
        set => SetProperty(ref _priceSeries, value);
    }

    public ObservableCollection<ISeries> KFactorSeries
    {
        get => _kFactorSeries;
        set => SetProperty(ref _kFactorSeries, value);
    }

    public Axis[] BurnRateXAxes
    {
        get => _burnRateXAxes;
        set => SetProperty(ref _burnRateXAxes, value);
    }

    public Axis[] BurnRateYAxes
    {
        get => _burnRateYAxes;
        set => SetProperty(ref _burnRateYAxes, value);
    }

    public Axis[] PriceXAxes
    {
        get => _priceXAxes;
        set => SetProperty(ref _priceXAxes, value);
    }

    public Axis[] PriceYAxes
    {
        get => _priceYAxes;
        set => SetProperty(ref _priceYAxes, value);
    }

    public Axis[] KFactorXAxes
    {
        get => _kFactorXAxes;
        set => SetProperty(ref _kFactorXAxes, value);
    }

    public Axis[] KFactorYAxes
    {
        get => _kFactorYAxes;
        set => SetProperty(ref _kFactorYAxes, value);
    }

    public TooltipFindingStrategy TooltipFindingStrategy
    {
        get => _tooltipFindingStrategy;
        set => SetProperty(ref _tooltipFindingStrategy, value);
    }

    public ChartsViewModel(IDataService dataService, IWeatherService weatherService)
    {
        _dataService = dataService;
        _weatherService = weatherService;
        InitializeAxes();
    }

    private void InitializeAxes()
    {
        BurnRateXAxes =
        [
            new DateTimeAxis(TimeSpan.FromDays(30), date => date.ToString("MMM yyyy"))
            {
                Name = "Date",
                NamePaint = new SolidColorPaint(SKColors.Gray),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                CrosshairPaint = CrosshairPaint,
                CrosshairSnapEnabled = true
            }
        ];

        BurnRateYAxes =
        [
            new Axis
            {
                Name = "Gallons per Day",
                NamePaint = new SolidColorPaint(SKColors.Gray),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                MinLimit = 0
            }
        ];

        PriceXAxes =
        [
            new DateTimeAxis(TimeSpan.FromDays(30), date => date.ToString("MMM yyyy"))
            {
                Name = "Date",
                NamePaint = new SolidColorPaint(SKColors.Gray),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                CrosshairPaint = CrosshairPaint,
                CrosshairSnapEnabled = true
            }
        ];

        PriceYAxes =
        [
            new Axis
            {
                Name = "Price per Gallon ($)",
                NamePaint = new SolidColorPaint(SKColors.Gray),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                Labeler = value => value.ToString("C2"),
                MinLimit = 0
            }
        ];

        KFactorXAxes =
        [
            new DateTimeAxis(TimeSpan.FromDays(30), date => date.ToString("MMM yyyy"))
            {
                Name = "Date",
                NamePaint = new SolidColorPaint(SKColors.Gray),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                CrosshairPaint = CrosshairPaint,
                CrosshairSnapEnabled = true
            }
        ];

        KFactorYAxes =
        [
            new Axis
            {
                Name = "K-Factor (HDD/gal)",
                NamePaint = new SolidColorPaint(SKColors.Gray),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                Labeler = value => value.ToString("F1"),
                MinLimit = 0
            }
        ];
    }

    private async Task LoadChartsAsync()
    {
        var deliveries = await _dataService.GetDeliveriesAsync();
        var weatherData = await _dataService.GetWeatherHistoryAsync();
        var sorted = deliveries.OrderBy(d => d.Date).ToList();

        HasData = sorted.Count > 0;

        if (!HasData)
        {
            BurnRateSeries = new ObservableCollection<ISeries>();
            PriceSeries = new ObservableCollection<ISeries>();
            KFactorSeries = new ObservableCollection<ISeries>();
            HasKFactorData = false;
            return;
        }

        // Build burn rate data points (skip first delivery - no previous reference)
        var burnRatePoints = new List<DateTimePoint>();
        var kFactorPoints = new List<DateTimePoint>();

        for (int i = 1; i < sorted.Count; i++)
        {
            var days = (sorted[i].Date - sorted[i - 1].Date).TotalDays;
            if (days > 0)
            {
                var gallonsPerDay = (double)(sorted[i].Gallons / (decimal)days);
                burnRatePoints.Add(new DateTimePoint(sorted[i].Date, gallonsPerDay));

                // Calculate K-Factor if weather data available
                // Only include when HDD > 200 to filter out unreliable summer data
                if (weatherData.Count > 0)
                {
                    var hdd = _weatherService.CalculateHDD(weatherData, sorted[i - 1].Date, sorted[i].Date);
                    if (hdd > 200)
                    {
                        var kFactor = (double)_weatherService.CalculateKFactor(sorted[i].Gallons, hdd);
                        kFactorPoints.Add(new DateTimePoint(sorted[i].Date, kFactor));
                    }
                }
            }
        }

        // Build price data points (all deliveries)
        var pricePoints = sorted
            .Select(d => new DateTimePoint(d.Date, (double)d.PricePerGallon))
            .ToList();

        BurnRateSeries = new ObservableCollection<ISeries>
        {
            new LineSeries<DateTimePoint>
            {
                Values = burnRatePoints,
                Name = "Gallons/Day",
                Fill = null,
                GeometrySize = 7,
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#2563eb")) { StrokeThickness = 2 },
                GeometryFill = new SolidColorPaint(SKColors.White),
                Stroke = new SolidColorPaint(SKColor.Parse("#2563eb")) { StrokeThickness = 3 },
                LineSmoothness = 0,
                YToolTipLabelFormatter = point => $"{point.Model?.Value:F1} gal/day"
            }
        };

        PriceSeries = new ObservableCollection<ISeries>
        {
            new LineSeries<DateTimePoint>
            {
                Values = pricePoints,
                Name = "$/Gallon",
                Fill = null,
                GeometrySize = 7,
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#16a34a")) { StrokeThickness = 2 },
                GeometryFill = new SolidColorPaint(SKColors.White),
                Stroke = new SolidColorPaint(SKColor.Parse("#16a34a")) { StrokeThickness = 3 },
                LineSmoothness = 0,
                YToolTipLabelFormatter = point => $"{point.Model?.Value:C3}/gal"
            }
        };

        HasKFactorData = kFactorPoints.Count > 0;

        KFactorSeries = new ObservableCollection<ISeries>
        {
            new LineSeries<DateTimePoint>
            {
                Values = kFactorPoints,
                Name = "K-Factor",
                Fill = null,
                GeometrySize = 7,
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#dc2626")) { StrokeThickness = 2 },
                GeometryFill = new SolidColorPaint(SKColors.White),
                Stroke = new SolidColorPaint(SKColor.Parse("#dc2626")) { StrokeThickness = 3 },
                LineSmoothness = 0,
                YToolTipLabelFormatter = point => $"{point.Model?.Value:F3} gal/HDD"
            }
        };
    }

    public void OnNavigatedTo(NavigationContext navigationContext) => _ = LoadChartsAsync();
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }
}
