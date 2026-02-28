using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace HeatingOilTracker.Maui.ViewModels;

public class ReferenceViewModel : INotifyPropertyChanged
{
    public ICommand OpenLinkCommand { get; }

    public ReferenceViewModel()
    {
        OpenLinkCommand = new Command<string>(async url =>
        {
            if (!string.IsNullOrEmpty(url))
                await Launcher.Default.OpenAsync(new Uri(url));
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}