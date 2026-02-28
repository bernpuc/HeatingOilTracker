using HeatingOilTracker.Maui.Views;

namespace HeatingOilTracker.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("DeliveryEditorPage", typeof(DeliveryEditorPage));
    }
}