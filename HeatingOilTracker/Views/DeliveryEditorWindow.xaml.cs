using System.Windows;
using System.Windows.Input;
using TextBox = System.Windows.Controls.TextBox;

namespace HeatingOilTracker.Views;

public partial class DeliveryEditorWindow : Window
{
    public DeliveryEditorWindow()
    {
        InitializeComponent();
    }

    private void TextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    private void TextBox_GotMouseCapture(object sender, MouseEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }
}
