using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HeatingOilTracker.Views;

public partial class WeatherView : UserControl
{
    public WeatherView()
    {
        InitializeComponent();

        TemperatureChart.MouseMove += (s, e) =>
            UpdateTooltip(TemperatureChart, TemperatureTooltip, TemperatureTooltipText, TemperatureTooltipTransform, e);

        TemperatureChart.MouseLeave += (_, _) =>
            TemperatureTooltip.Visibility = Visibility.Collapsed;

        // Prevent mouse wheel from bubbling (let chart handle zoom)
        TemperatureChart.MouseWheel += (_, e) => e.Handled = true;
    }

    private void UpdateTooltip(
        CartesianChart chart,
        Border tooltip,
        TextBlock tooltipText,
        TranslateTransform transform,
        MouseEventArgs e)
    {
        var pos = e.GetPosition(chart);

        // Get all three series (High, Average, Low)
        var seriesList = chart.Series?.OfType<LineSeries<DateTimePoint>>().ToList();
        if (seriesList == null || seriesList.Count < 3)
        {
            tooltip.Visibility = Visibility.Collapsed;
            return;
        }

        var highSeries = seriesList[0].Values as IEnumerable<DateTimePoint>;
        var avgSeries = seriesList[1].Values as IEnumerable<DateTimePoint>;
        var lowSeries = seriesList[2].Values as IEnumerable<DateTimePoint>;

        if (avgSeries == null)
        {
            tooltip.Visibility = Visibility.Collapsed;
            return;
        }

        var avgPoints = avgSeries.ToList();
        var highPoints = highSeries?.ToList() ?? [];
        var lowPoints = lowSeries?.ToList() ?? [];

        if (avgPoints.Count == 0)
        {
            tooltip.Visibility = Visibility.Collapsed;
            return;
        }

        try
        {
            var dataCoords = chart.ScalePixelsToData(new LvcPointD(pos.X, pos.Y));
            var hoverTicks = (long)dataCoords.X;

            // Find nearest point by date
            var nearestAvg = avgPoints.MinBy(p => Math.Abs(p.DateTime.Ticks - hoverTicks));
            if (nearestAvg == null)
            {
                tooltip.Visibility = Visibility.Collapsed;
                return;
            }

            var idx = avgPoints.IndexOf(nearestAvg);
            var high = idx < highPoints.Count ? highPoints[idx].Value : null;
            var low = idx < lowPoints.Count ? lowPoints[idx].Value : null;

            // Format tooltip text
            var dateStr = nearestAvg.DateTime.ToString("MMM d, yyyy");
            var lines = new List<string> { dateStr };

            if (high.HasValue) lines.Add($"High: {high:F1}°");
            lines.Add($"Avg: {nearestAvg.Value:F1}°");
            if (low.HasValue) lines.Add($"Low: {low:F1}°");

            tooltipText.Text = string.Join("   ", lines);
            tooltip.Visibility = Visibility.Visible;

            // Get pixel X of the nearest data point so tooltip snaps with the crosshair
            var nearestPixel = chart.ScaleDataToPixels(
                new LvcPointD(nearestAvg.DateTime.Ticks, nearestAvg.Value ?? 0));

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
