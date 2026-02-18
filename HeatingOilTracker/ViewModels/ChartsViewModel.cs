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
    private bool _hasDeliveryComparisonData;
    private ObservableCollection<ISeries> _burnRateSeries = new();
    private ObservableCollection<ISeries> _priceSeries = new();
    private ObservableCollection<ISeries> _kFactorSeries = new();
    private ObservableCollection<ISeries> _deliveryComparisonSeries = new();
    private Axis[] _burnRateXAxes = [];
    private Axis[] _burnRateYAxes = [];
    private Axis[] _priceXAxes = [];
    private Axis[] _priceYAxes = [];
    private Axis[] _kFactorXAxes = [];
    private Axis[] _kFactorYAxes = [];
    private Axis[] _deliveryComparisonXAxes = [];
    private Axis[] _deliveryComparisonYAxes = [];
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

    public bool HasDeliveryComparisonData
    {
        get => _hasDeliveryComparisonData;
        set => SetProperty(ref _hasDeliveryComparisonData, value);
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

    public ObservableCollection<ISeries> DeliveryComparisonSeries
    {
        get => _deliveryComparisonSeries;
        set => SetProperty(ref _deliveryComparisonSeries, value);
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

    public Axis[] DeliveryComparisonXAxes
    {
        get => _deliveryComparisonXAxes;
        set => SetProperty(ref _deliveryComparisonXAxes, value);
    }

    public Axis[] DeliveryComparisonYAxes
    {
        get => _deliveryComparisonYAxes;
        set => SetProperty(ref _deliveryComparisonYAxes, value);
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

        DeliveryComparisonXAxes =
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

        DeliveryComparisonYAxes =
        [
            new Axis
            {
                Name = "Gallons",
                NamePaint = new SolidColorPaint(SKColors.Gray),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                Labeler = value => value.ToString("F0")
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
            DeliveryComparisonSeries = new ObservableCollection<ISeries>();
            HasKFactorData = false;
            HasDeliveryComparisonData = false;
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

        // Build Delivery Comparison chart (Actual Delivered vs Estimated Use)
        var actualDeliveredPoints = new List<DateTimePoint>();
        var estimatedHddPoints = new List<DateTimePoint>();
        var estimatedBurnRatePoints = new List<DateTimePoint>();

        // Calculate separate burn rates for heating season (Oct-Mar) and off-season (Apr-Sep)
        var offSeasonRates = new List<double>();
        var heatingSeasonRates = new List<double>();

        for (int i = 1; i < sorted.Count; i++)
        {
            var days = (sorted[i].Date - sorted[i - 1].Date).TotalDays;
            if (days > 0)
            {
                var rate = (double)sorted[i].Gallons / days;
                var month = sorted[i].Date.Month;
                // Off-season: Apr (4) through Sep (9)
                // Check if delivery period is entirely within off-season
                var prevMonth = sorted[i - 1].Date.Month;
                var isOffSeasonDelivery = month >= 4 && month <= 9 && prevMonth >= 4 && prevMonth <= 9;

                if (isOffSeasonDelivery)
                {
                    offSeasonRates.Add(rate);
                }
                else if (month >= 10 || month <= 3)
                {
                    heatingSeasonRates.Add(rate);
                }
            }
        }

        var avgOffSeasonBurnRate = offSeasonRates.Count > 0 ? offSeasonRates.Average() : 0;
        var avgHeatingSeasonBurnRate = heatingSeasonRates.Count > 0 ? heatingSeasonRates.Average() : 0;

        if (weatherData.Count > 0 && kFactorPoints.Count > 0)
        {
            // Calculate average K-Factor from valid periods
            var avgKFactor = kFactorPoints.Average(p => p.Value ?? 0);

            for (int i = 1; i < sorted.Count; i++)
            {
                var hdd = _weatherService.CalculateHDD(weatherData, sorted[i - 1].Date, sorted[i].Date);
                var days = (sorted[i].Date - sorted[i - 1].Date).TotalDays;
                var month = sorted[i].Date.Month;
                var isOffSeason = month >= 4 && month <= 9;

                // Add actual delivered
                actualDeliveredPoints.Add(new DateTimePoint(sorted[i].Date, (double)sorted[i].Gallons));

                // Calculate estimated use based on HDD and average K-Factor
                if (avgKFactor > 0 && hdd > 0)
                {
                    var estimatedUse = (double)hdd / avgKFactor;
                    estimatedHddPoints.Add(new DateTimePoint(sorted[i].Date, estimatedUse));
                }
                else
                {
                    estimatedHddPoints.Add(new DateTimePoint(sorted[i].Date, null));
                }

                // Calculate estimated use based on seasonal burn rate
                var burnRate = isOffSeason ? avgOffSeasonBurnRate : avgHeatingSeasonBurnRate;
                if (burnRate > 0 && days > 0)
                {
                    var estimatedBurnRate = burnRate * days;
                    estimatedBurnRatePoints.Add(new DateTimePoint(sorted[i].Date, estimatedBurnRate));
                }
                else
                {
                    estimatedBurnRatePoints.Add(new DateTimePoint(sorted[i].Date, null));
                }
            }
        }

        HasDeliveryComparisonData = actualDeliveredPoints.Count > 0;

        DeliveryComparisonSeries = new ObservableCollection<ISeries>
        {
            new ColumnSeries<DateTimePoint>
            {
                Values = actualDeliveredPoints,
                Name = "Actual Delivered",
                Fill = new SolidColorPaint(SKColor.Parse("#2563eb")),
                MaxBarWidth = 6,
                YToolTipLabelFormatter = point => $"{point.Model?.Value:F1} gal"
            },
            new ColumnSeries<DateTimePoint>
            {
                Values = estimatedHddPoints,
                Name = "Est. (HDD)",
                Fill = new SolidColorPaint(SKColor.Parse("#f97316")),
                MaxBarWidth = 6,
                YToolTipLabelFormatter = point => $"{point.Model?.Value:F1} gal"
            },
            new ColumnSeries<DateTimePoint>
            {
                Values = estimatedBurnRatePoints,
                Name = "Est. (Burn Rate)",
                Fill = new SolidColorPaint(SKColor.Parse("#10b981")),
                MaxBarWidth = 6,
                YToolTipLabelFormatter = point => $"{point.Model?.Value:F1} gal"
            }
        };
    }

    public void OnNavigatedTo(NavigationContext navigationContext) => _ = LoadChartsAsync();
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }
}
