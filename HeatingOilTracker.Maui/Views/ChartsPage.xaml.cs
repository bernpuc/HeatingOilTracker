using HeatingOilTracker.Maui.ViewModels;
using LiveChartsCore.Drawing;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Maui;

namespace HeatingOilTracker.Maui.Views;

public partial class ChartsPage : ContentPage
{
    private readonly ChartsViewModel _viewModel;

#if WINDOWS
    // Stores the full-data X range for each chart, captured on the first zoom interaction
    // while limits are still null. Used to prevent zooming out past the natural extent.
    private readonly Dictionary<CartesianChart, (double Min, double Max)> _naturalRanges = new();
#endif

    public ChartsPage(ChartsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

#if WINDOWS
        BurnRateOverlay.HandlerChanged           += (_, _) => AttachWindowsZoom(BurnRateOverlay,           BurnRateChart);
        PriceOverlay.HandlerChanged              += (_, _) => AttachWindowsZoom(PriceOverlay,              PriceChart);
        KFactorOverlay.HandlerChanged            += (_, _) => AttachWindowsZoom(KFactorOverlay,            KFactorChart);
        DeliveryComparisonOverlay.HandlerChanged += (_, _) => AttachWindowsZoom(DeliveryComparisonOverlay, DeliveryComparisonChart);
#endif
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
#if WINDOWS
        // Clear cached ranges so a data reload captures fresh extents.
        _naturalRanges.Clear();
#endif
        _ = _viewModel.LoadChartsAsync();
    }

#if WINDOWS
    private void AttachWindowsZoom(Grid overlay, CartesianChart chart)
    {
        if (overlay.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement platformView)
            return;

        platformView.PointerWheelChanged += (_, args) =>
        {
            var point = args.GetCurrentPoint(platformView);
            var delta = point.Properties.MouseWheelDelta;

            if (args.KeyModifiers.HasFlag(Windows.System.VirtualKeyModifiers.Control))
            {
                args.Handled = true;
                ZoomChartX(chart, point.Position.X, delta > 0);
            }
            // No Ctrl: don't mark handled — bubbles to ScrollViewer → normal scroll.
        };

        platformView.DoubleTapped += (_, _) =>
        {
            foreach (var axis in chart.XAxes.OfType<Axis>())
                axis.MinLimit = axis.MaxLimit = null;
        };
    }

    private void ZoomChartX(CartesianChart chart, double pivotPixelX, bool zoomIn)
    {
        var axis = chart.XAxes?.OfType<Axis>().FirstOrDefault();
        if (axis == null) return;

        var w = chart.Width;
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
                // If the next zoom-out step would meet or exceed the full data range, reset instead.
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
                return; // already at full view, no natural range stored yet
            }
        }

        var leftData  = chart.ScalePixelsToData(new LvcPointD(0, 0)).X;
        var rightData = chart.ScalePixelsToData(new LvcPointD(w, 0)).X;
        var pivotData = chart.ScalePixelsToData(new LvcPointD(pivotPixelX, 0)).X;

        var range      = rightData - leftData;
        if (range <= 0) return;

        var factor     = zoomIn ? 0.75 : 1.333;
        var newRange   = range * factor;
        var pivotRatio = (pivotData - leftData) / range;
        axis.MinLimit  = pivotData - newRange * pivotRatio;
        axis.MaxLimit  = pivotData + newRange * (1 - pivotRatio);
    }
#endif
}
