using Android.Views;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace HeatingOilTracker.Maui;

public class CustomScrollViewHandler : ScrollViewHandler
{
    protected override void ConnectHandler(MauiScrollView platformView)
    {
        base.ConnectHandler(platformView);
        platformView.OverScrollMode = OverScrollMode.Never;
    }
}