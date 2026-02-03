using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace HeatingOilTracker.Views;

public partial class ReferenceView : UserControl
{
    public ReferenceView()
    {
        InitializeComponent();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
