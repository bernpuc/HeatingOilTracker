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
    public ChartsView()
    {
        InitializeComponent();

        BurnRateChart.MouseMove += (s, e) =>
            UpdateTooltip(BurnRateChart, BurnRateTooltip, BurnRateTooltipText, BurnRateTooltipTransform, e,
                p => $"{p.DateTime:MMM d, yyyy}   {p.Value:F1} gal/day");

        BurnRateChart.MouseLeave += (_, _) =>
            BurnRateTooltip.Visibility = Visibility.Collapsed;

        PriceChart.MouseMove += (s, e) =>
            UpdateTooltip(PriceChart, PriceTooltip, PriceTooltipText, PriceTooltipTransform, e,
                p => $"{p.DateTime:MMM d, yyyy}   {p.Value:C3}/gal");

        PriceChart.MouseLeave += (_, _) =>
            PriceTooltip.Visibility = Visibility.Collapsed;

        KFactorChart.MouseMove += (s, e) =>
            UpdateTooltip(KFactorChart, KFactorTooltip, KFactorTooltipText, KFactorTooltipTransform, e,
                p => $"{p.DateTime:MMM d, yyyy}   {p.Value:F3} gal/HDD");

        KFactorChart.MouseLeave += (_, _) =>
            KFactorTooltip.Visibility = Visibility.Collapsed;

        DeliveryComparisonChart.MouseMove += (s, e) =>
            UpdateBarChartTooltip(DeliveryComparisonChart, DeliveryComparisonTooltip, DeliveryComparisonTooltipText, DeliveryComparisonTooltipTransform, e);

        DeliveryComparisonChart.MouseLeave += (_, _) =>
            DeliveryComparisonTooltip.Visibility = Visibility.Collapsed;

        // Prevent mouse wheel from bubbling to ScrollViewer (let charts handle zoom)
        BurnRateChart.MouseWheel += (_, e) => e.Handled = true;
        PriceChart.MouseWheel += (_, e) => e.Handled = true;
        KFactorChart.MouseWheel += (_, e) => e.Handled = true;
        DeliveryComparisonChart.MouseWheel += (_, e) => e.Handled = true;
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

            // Get pixel X of the nearest data point so tooltip snaps with the crosshair
            var nearestPixel = chart.ScaleDataToPixels(
                new LvcPointD(nearest.DateTime.Ticks, nearest.Value ?? 0));

            tooltipText.Text = formatter(nearest);
            tooltip.Visibility = Visibility.Visible;

            // Measure to get width for centering
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

        var actualSeries = seriesList[0];
        var hddSeries = seriesList[1];
        var burnRateSeries = seriesList[2];

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

            // Find matching HDD estimate point
            double? hddValue = null;
            if (hddSeries.Values is IEnumerable<DateTimePoint> hddValues)
            {
                var hddPoints = hddValues.ToList();
                var nearestHdd = hddPoints.FirstOrDefault(p => p.DateTime == nearestActual.DateTime);
                hddValue = nearestHdd?.Value;
            }

            // Find matching burn rate estimate point
            double? burnRateValue = null;
            if (burnRateSeries.Values is IEnumerable<DateTimePoint> burnRateValues)
            {
                var burnRatePoints = burnRateValues.ToList();
                var nearestBurnRate = burnRatePoints.FirstOrDefault(p => p.DateTime == nearestActual.DateTime);
                burnRateValue = nearestBurnRate?.Value;
            }

            // Get pixel X of the nearest data point so tooltip snaps with the crosshair
            var nearestPixel = chart.ScaleDataToPixels(
                new LvcPointD(nearestActual.DateTime.Ticks, nearestActual.Value ?? 0));

            var actualStr = nearestActual.Value.HasValue ? $"{nearestActual.Value:F1}" : "N/A";
            var hddStr = hddValue.HasValue ? $"{hddValue:F1}" : "N/A";
            var burnRateStr = burnRateValue.HasValue ? $"{burnRateValue:F1}" : "N/A";
            tooltipText.Text = $"{nearestActual.DateTime:MMM d, yyyy}\nActual: {actualStr} gal\nEst (HDD): {hddStr} gal\nEst (Burn): {burnRateStr} gal";
            tooltip.Visibility = Visibility.Visible;

            // Measure to get width for centering
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
