using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HeatingOilTracker.Views;

public partial class ChartsView : UserControl
{
    private readonly Dictionary<CartesianChart, (double Min, double Max)> _naturalRanges = new();

    public ChartsView()
    {
        InitializeComponent();

        // Tooltips: attached to overlays (charts are behind overlays and won't receive mouse events)
        BurnRateOverlay.MouseMove += (s, e) =>
            UpdateTooltip(BurnRateChart, BurnRateTooltip, BurnRateTooltipText, BurnRateTooltipTransform, e,
                p => $"{p.DateTime:MMM d, yyyy}   {p.Value:F1} gal/day");

        BurnRateOverlay.MouseLeave += (_, _) =>
            BurnRateTooltip.Visibility = Visibility.Collapsed;

        PriceOverlay.MouseMove += (s, e) =>
            UpdateTooltip(PriceChart, PriceTooltip, PriceTooltipText, PriceTooltipTransform, e,
                p => $"{p.DateTime:MMM d, yyyy}   {p.Value:C3}/gal");

        PriceOverlay.MouseLeave += (_, _) =>
            PriceTooltip.Visibility = Visibility.Collapsed;

        KFactorOverlay.MouseMove += (s, e) =>
            UpdateTooltip(KFactorChart, KFactorTooltip, KFactorTooltipText, KFactorTooltipTransform, e,
                p => $"{p.DateTime:MMM d, yyyy}   {p.Value:F3} gal/HDD");

        KFactorOverlay.MouseLeave += (_, _) =>
            KFactorTooltip.Visibility = Visibility.Collapsed;

        DeliveryComparisonOverlay.MouseMove += (s, e) =>
            UpdateBarChartTooltip(DeliveryComparisonChart, DeliveryComparisonTooltip, DeliveryComparisonTooltipText, DeliveryComparisonTooltipTransform, e);

        DeliveryComparisonOverlay.MouseLeave += (_, _) =>
            DeliveryComparisonTooltip.Visibility = Visibility.Collapsed;

        // Wheel: intercepted at ScrollViewer level (above all charts in the tunnel).
        // Because overlays are the hit-test targets the charts never receive mouse events
        // at all, so LiveCharts' internal handlers never fire.
        ChartsScrollViewer.PreviewMouseWheel += (_, e) =>
        {
            var chart = GetHoveredChart(e);
            if (chart == null) return;

            e.Handled = true;

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                var pos = e.GetPosition(chart);
                ZoomChartX(chart, pos.X, zoomIn: e.Delta > 0);
            }
            else
            {
                ChartsScrollViewer.ScrollToVerticalOffset(
                    ChartsScrollViewer.VerticalOffset - e.Delta / 3.0);
            }
        };

        // Double-click to reset zoom
        foreach (var (overlay, chart) in new[]
        {
            (BurnRateOverlay,           (CartesianChart)BurnRateChart),
            (PriceOverlay,              PriceChart),
            (KFactorOverlay,            KFactorChart),
            (DeliveryComparisonOverlay, DeliveryComparisonChart),
        })
        {
            var c = chart;
            overlay.MouseLeftButtonDown += (_, e) =>
            {
                if (e.ClickCount == 2)
                {
                    foreach (var axis in c.XAxes.OfType<Axis>())
                        axis.MinLimit = axis.MaxLimit = null;
                }
            };
        }
    }

    private CartesianChart? GetHoveredChart(MouseEventArgs e)
    {
        foreach (var chart in new[] { BurnRateChart, PriceChart, KFactorChart, DeliveryComparisonChart })
        {
            if (!chart.IsVisible) continue;
            var pos = e.GetPosition(chart);
            if (pos.X >= 0 && pos.X <= chart.ActualWidth && pos.Y >= 0 && pos.Y <= chart.ActualHeight)
                return chart;
        }
        return null;
    }

    private void ZoomChartX(CartesianChart chart, double pivotPixelX, bool zoomIn)
    {
        var axis = chart.XAxes?.OfType<Axis>().FirstOrDefault();
        if (axis == null) return;

        var w = chart.ActualWidth;
        if (w <= 0) return;

        // Capture the natural full-data range the first time we zoom while at auto-fit.
        if (!_naturalRanges.ContainsKey(chart) && axis.MinLimit == null && axis.MaxLimit == null)
        {
            var nl = chart.ScalePixelsToData(new LvcPointD(0, 0)).X;
            var nr = chart.ScalePixelsToData(new LvcPointD(w, 0)).X;
            if (nr > nl) _naturalRanges[chart] = (nl, nr);
        }

        if (!zoomIn)
        {
            if (_naturalRanges.TryGetValue(chart, out var nat))
            {
                var currentRange = chart.ScalePixelsToData(new LvcPointD(w, 0)).X
                                 - chart.ScalePixelsToData(new LvcPointD(0, 0)).X;
                if (currentRange * 1.333 >= nat.Max - nat.Min)
                {
                    axis.MinLimit = null;
                    axis.MaxLimit = null;
                    return;
                }
            }
            else if (axis.MinLimit == null)
            {
                return;
            }
        }

        var leftData  = chart.ScalePixelsToData(new LvcPointD(0, 0)).X;
        var rightData = chart.ScalePixelsToData(new LvcPointD(w, 0)).X;
        var pivotData = chart.ScalePixelsToData(new LvcPointD(pivotPixelX, 0)).X;

        var range = rightData - leftData;
        if (range <= 0) return;

        var factor     = zoomIn ? 0.75 : 1.333;
        var newRange   = range * factor;
        var pivotRatio = (pivotData - leftData) / range;
        axis.MinLimit  = pivotData - newRange * pivotRatio;
        axis.MaxLimit  = pivotData + newRange * (1 - pivotRatio);
    }

    private static void UpdateTooltip(
        CartesianChart chart,
        Border tooltip,
        TextBlock tooltipText,
        TranslateTransform transform,
        MouseEventArgs e,
        Func<DateTimePoint, string> formatter)
    {
        var pos = e.GetPosition(chart);

        var series = chart.Series?.OfType<LineSeries<DateTimePoint>>().FirstOrDefault();
        if (series?.Values is not IEnumerable<DateTimePoint> values)
        {
            tooltip.Visibility = Visibility.Collapsed;
            return;
        }

        var points = values.ToList();
        if (points.Count == 0)
        {
            tooltip.Visibility = Visibility.Collapsed;
            return;
        }

        try
        {
            var dataCoords = chart.ScalePixelsToData(new LvcPointD(pos.X, pos.Y));
            var hoverTicks = (long)dataCoords.X;

            var nearest = points.MinBy(p => Math.Abs(p.DateTime.Ticks - hoverTicks));
            if (nearest == null)
            {
                tooltip.Visibility = Visibility.Collapsed;
                return;
            }

            var nearestPixel = chart.ScaleDataToPixels(
                new LvcPointD(nearest.DateTime.Ticks, nearest.Value ?? 0));

            tooltipText.Text = formatter(nearest);
            tooltip.Visibility = Visibility.Visible;

            tooltip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var tooltipWidth = tooltip.DesiredSize.Width;

            var x = nearestPixel.X - tooltipWidth / 2;
            x = Math.Max(0, Math.Min(x, chart.ActualWidth - tooltipWidth));

            transform.X = x;
        }
        catch
        {
            tooltip.Visibility = Visibility.Collapsed;
        }
    }

    private static void UpdateBarChartTooltip(
        CartesianChart chart,
        Border tooltip,
        TextBlock tooltipText,
        TranslateTransform transform,
        MouseEventArgs e)
    {
        var pos = e.GetPosition(chart);

        var seriesList = chart.Series?.OfType<ColumnSeries<DateTimePoint>>().ToList();
        if (seriesList == null || seriesList.Count < 3)
        {
            tooltip.Visibility = Visibility.Collapsed;
            return;
        }

        var actualSeries    = seriesList[0];
        var hddSeries       = seriesList[1];
        var burnRateSeries  = seriesList[2];

        if (actualSeries.Values is not IEnumerable<DateTimePoint> actualValues)
        {
            tooltip.Visibility = Visibility.Collapsed;
            return;
        }

        var actualPoints = actualValues.ToList();
        if (actualPoints.Count == 0)
        {
            tooltip.Visibility = Visibility.Collapsed;
            return;
        }

        try
        {
            var dataCoords = chart.ScalePixelsToData(new LvcPointD(pos.X, pos.Y));
            var hoverTicks = (long)dataCoords.X;

            var nearestActual = actualPoints.MinBy(p => Math.Abs(p.DateTime.Ticks - hoverTicks));
            if (nearestActual == null)
            {
                tooltip.Visibility = Visibility.Collapsed;
                return;
            }

            double? hddValue = null;
            if (hddSeries.Values is IEnumerable<DateTimePoint> hddValues)
            {
                var nearestHdd = hddValues.FirstOrDefault(p => p.DateTime == nearestActual.DateTime);
                hddValue = nearestHdd?.Value;
            }

            double? burnRateValue = null;
            if (burnRateSeries.Values is IEnumerable<DateTimePoint> burnRateValues)
            {
                var nearestBurnRate = burnRateValues.FirstOrDefault(p => p.DateTime == nearestActual.DateTime);
                burnRateValue = nearestBurnRate?.Value;
            }

            var nearestPixel = chart.ScaleDataToPixels(
                new LvcPointD(nearestActual.DateTime.Ticks, nearestActual.Value ?? 0));

            var actualStr    = nearestActual.Value.HasValue ? $"{nearestActual.Value:F1}" : "N/A";
            var hddStr       = hddValue.HasValue ? $"{hddValue:F1}" : "N/A";
            var burnRateStr  = burnRateValue.HasValue ? $"{burnRateValue:F1}" : "N/A";
            tooltipText.Text = $"{nearestActual.DateTime:MMM d, yyyy}\nActual: {actualStr} gal\nEst (HDD): {hddStr} gal\nEst (Burn): {burnRateStr} gal";
            tooltip.Visibility = Visibility.Visible;

            tooltip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var tooltipWidth = tooltip.DesiredSize.Width;

            var x = nearestPixel.X - tooltipWidth / 2;
            x = Math.Max(0, Math.Min(x, chart.ActualWidth - tooltipWidth));

            transform.X = x;
        }
        catch
        {
            tooltip.Visibility = Visibility.Collapsed;
        }
    }
}
