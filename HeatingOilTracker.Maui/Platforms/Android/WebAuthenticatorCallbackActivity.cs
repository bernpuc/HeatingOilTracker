using Android.App;
using Android.Content.PM;

namespace HeatingOilTracker.Maui;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    [Android.Content.Intent.ActionView],
    Categories = [Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable],
    DataScheme = "com.bernpuc.heatingoiltracker")]
public class WebAuthenticatorCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
}
