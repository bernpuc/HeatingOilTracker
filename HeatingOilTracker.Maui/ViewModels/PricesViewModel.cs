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

namespace HeatingOilTracker.Maui.ViewModels;

public class PricesViewModel : INotifyPropertyChanged
{
    private readonly IDataService _dataService;
    private readonly IEiaService _eiaService;

    private bool _hasData;
    private bool _isLoading;
    private bool _hasError;
    private string _regionName = string.Empty;
    private string _latestRegionPrice = string.Empty;
    private string _latestNationalPrice = string.Empty;
    private string _priceDiff = string.Empty;
    private string _trendSummary = string.Empty;
    private string _suggestion = string.Empty;
    private string _errorMessage = string.Empty;
    private ObservableCollection<ISeries> _priceSeries = [];
    private Axis[] _xAxes = [];
    private Axis[] _yAxes = [];

    public bool HasData    { get => _hasData;    set => SetProperty(ref _hasData, value); }
    public bool IsLoading  { get => _isLoading;  set => SetProperty(ref _isLoading, value); }
    public bool HasError   { get => _hasError;   set => SetProperty(ref _hasError, value); }
    public string RegionName         { get => _regionName;         set => SetProperty(ref _regionName, value); }
    public string LatestRegionPrice  { get => _latestRegionPrice;  set => SetProperty(ref _latestRegionPrice, value); }
    public string LatestNationalPrice{ get => _latestNationalPrice; set => SetProperty(ref _latestNationalPrice, value); }
    public string PriceDiff          { get => _priceDiff;          set => SetProperty(ref _priceDiff, value); }
    public string TrendSummary       { get => _trendSummary;       set => SetProperty(ref _trendSummary, value); }
    public string Suggestion         { get => _suggestion;         set => SetProperty(ref _suggestion, value); }
    public string ErrorMessage       { get => _errorMessage;       set => SetProperty(ref _errorMessage, value); }
    public ObservableCollection<ISeries> PriceSeries { get => _priceSeries; set => SetProperty(ref _priceSeries, value); }
    public Axis[] XAxes { get => _xAxes; set => SetProperty(ref _xAxes, value); }
    public Axis[] YAxes { get => _yAxes; set => SetProperty(ref _yAxes, value); }

    public Command RefreshCommand { get; }

    public PricesViewModel(IDataService dataService, IEiaService eiaService)
    {
        _dataService = dataService;
        _eiaService = eiaService;
        RefreshCommand = new Command(async () => await LoadAsync());
        InitializeAxes();
    }

    private void InitializeAxes()
    {
        XAxes =
        [
            new DateTimeAxis(TimeSpan.FromDays(7), date => date.ToString("MMM d"))
            {
                NamePaint = new SolidColorPaint(SKColors.Gray),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                CrosshairPaint = new SolidColorPaint(SKColor.Parse("#94a3b8"))
                {
                    StrokeThickness = 1,
                    PathEffect = new DashEffect([4, 4])
                },
                CrosshairSnapEnabled = true
            }
        ];

        YAxes =
        [
            new Axis
            {
                Name = "$/gal",
                NamePaint = new SolidColorPaint(SKColors.Gray),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                Labeler = v => ((decimal)v).ToString("C3"),
                MinLimit = 0
            }
        ];
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasData = false;
        HasError = false;

        try
        {
            var regionalSettings = await _dataService.GetRegionalSettingsAsync();
            var location = await _dataService.GetLocationAsync();

            var regionCode = !string.IsNullOrWhiteSpace(regionalSettings.EiaRegionCode)
                ? regionalSettings.EiaRegionCode
                : location.IsSet
                    ? EiaRegionMapper.FromCoordinates(location.Latitude, location.Longitude)
                    : EiaRegion.USAverage;

            RegionName = EiaRegion.GetName(regionCode);

            var regionPrices = await _eiaService.GetWeeklyPricesAsync(regionCode, weeks: 56);
            var nationalPrices = regionCode == EiaRegion.USAverage
                ? regionPrices
                : await _eiaService.GetWeeklyPricesAsync(EiaRegion.USAverage, weeks: 56);

            if (regionPrices.Count == 0)
            {
                ErrorMessage = "No price data returned. Verify your EIA API key in Settings → API Keys.";
                HasError = true;
                return;
            }

            var latestRegion   = regionPrices[0].PricePerGallon;
            var latestNational = nationalPrices.Count > 0 ? nationalPrices[0].PricePerGallon : latestRegion;

            LatestRegionPrice   = latestRegion.ToString("C3");
            LatestNationalPrice = latestNational.ToString("C3");

            if (regionCode != EiaRegion.USAverage && nationalPrices.Count > 0)
            {
                var diff    = latestRegion - latestNational;
                var diffPct = latestNational > 0 ? diff / latestNational * 100m : 0m;
                var sign    = diff >= 0 ? "+" : "";
                PriceDiff = $"Your region is {sign}{diff:C3} ({sign}{diffPct:F1}%) vs. national avg";
            }
            else
            {
                PriceDiff = "Showing U.S. national average";
            }

            ComputeTrend(regionPrices, nationalPrices, regionCode);
            BuildChart(regionPrices, nationalPrices, regionCode);

            HasData = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex is HttpRequestException
                ? "Could not reach the EIA API. Check your API key in Settings → API Keys."
                : $"Error loading price data: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ComputeTrend(List<EiaHeatingOilPrice> region, List<EiaHeatingOilPrice> national, string regionCode)
    {
        if (region.Count < 4)
        {
            TrendSummary = "Not enough data for trend analysis.";
            Suggestion = string.Empty;
            return;
        }

        var avg4  = region.Take(4).Average(p => p.PricePerGallon);
        var avg13 = region.Take(Math.Min(13, region.Count)).Average(p => p.PricePerGallon);

        var trendDir = avg4 > avg13 * 1.02m ? "up"
                     : avg4 < avg13 * 0.98m ? "down"
                     : "stable";

        var weekChange = region.Count >= 2 ? region[0].PricePerGallon - region[1].PricePerGallon : 0m;
        var weekPct    = region.Count >= 2 && region[1].PricePerGallon > 0
                         ? weekChange / region[1].PricePerGallon * 100m : 0m;
        var weekStr = weekChange == 0m ? "unchanged week-over-week"
                    : weekChange > 0   ? $"+{weekPct:F1}% week-over-week"
                    : $"{weekPct:F1}% week-over-week";

        var yoyStr = string.Empty;
        if (region.Count >= 52)
        {
            var yoy    = region[0].PricePerGallon - region[51].PricePerGallon;
            var yoyPct = region[51].PricePerGallon > 0 ? yoy / region[51].PricePerGallon * 100m : 0m;
            yoyStr = $" · {(yoy >= 0 ? "+" : "")}{yoyPct:F1}% year-over-year";
        }

        var trendLabel = trendDir == "up" ? "Trending up" : trendDir == "down" ? "Trending down" : "Stable";
        TrendSummary = $"{trendLabel} · {weekStr}{yoyStr}";

        var aboveNational = regionCode != EiaRegion.USAverage
                            && national.Count > 0
                            && region[0].PricePerGallon > national[0].PricePerGallon;

        Suggestion = (trendDir, regionCode == EiaRegion.USAverage, aboveNational) switch
        {
            ("up",     false, true)  => "Prices are rising and above the national average. Consider ordering soon before costs climb further.",
            ("up",     false, false) => "Prices are rising but still below the national average. Monitor weekly — order before they reach the national level.",
            ("down",   false, true)  => "Prices are falling from elevated levels. Waiting a few weeks may save money.",
            ("down",   false, false) => "Prices are falling and already below the national average. Good time to fill up if your tank is running low.",
            ("stable", false, true)  => "Prices are stable but above the national average. Shop around for competitive quotes.",
            ("stable", false, false) => "Prices are stable and below the national average. Solid value — order at your convenience.",
            ("up",     true,  _)     => "National prices are trending up. Consider ordering soon to lock in the current price.",
            ("down",   true,  _)     => "National prices are trending down. You may benefit from waiting a few weeks.",
            _                        => "Prices are relatively stable. Order based on your tank level and schedule."
        };
    }

    private void BuildChart(List<EiaHeatingOilPrice> region, List<EiaHeatingOilPrice> national, string regionCode)
    {
        var regionPoints = region
            .Select(p => new DateTimePoint(p.Period.ToDateTime(TimeOnly.MinValue), (double)p.PricePerGallon))
            .OrderBy(p => p.DateTime)
            .ToList();

        var series = new ObservableCollection<ISeries>
        {
            new LineSeries<DateTimePoint>
            {
                Values = regionPoints,
                Name = RegionName,
                Fill = null,
                GeometrySize = 8,
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#2563eb")) { StrokeThickness = 2 },
                GeometryFill = new SolidColorPaint(SKColors.White),
                Stroke = new SolidColorPaint(SKColor.Parse("#2563eb")) { StrokeThickness = 2 },
                LineSmoothness = 0.3,
                YToolTipLabelFormatter = p => $"{p.Model?.Value:C3}/gal"
            }
        };

        if (regionCode != EiaRegion.USAverage && national.Count > 0)
        {
            var nationalPoints = national
                .Select(p => new DateTimePoint(p.Period.ToDateTime(TimeOnly.MinValue), (double)p.PricePerGallon))
                .OrderBy(p => p.DateTime)
                .ToList();

            series.Add(new LineSeries<DateTimePoint>
            {
                Values = nationalPoints,
                Name = "U.S. Average",
                Fill = null,
                GeometrySize = 0,
                Stroke = new SolidColorPaint(SKColor.Parse("#94a3b8"))
                {
                    StrokeThickness = 1.5f,
                    PathEffect = new DashEffect([6, 3])
                },
                LineSmoothness = 0.3,
                YToolTipLabelFormatter = p => $"{p.Model?.Value:C3}/gal"
            });
        }

        PriceSeries = series;
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
