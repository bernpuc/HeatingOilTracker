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
}
