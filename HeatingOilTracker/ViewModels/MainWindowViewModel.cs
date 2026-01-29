using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System.Windows;

namespace HeatingOilTracker.ViewModels;

public class MainWindowViewModel : BindableBase
{
    private readonly IRegionManager _regionManager;

    public DelegateCommand<string> NavigateCommand { get; }
    public DelegateCommand ExitCommand { get; }

    public MainWindowViewModel(IRegionManager regionManager)
    {
        _regionManager = regionManager;

        NavigateCommand = new DelegateCommand<string>(Navigate);
        ExitCommand = new DelegateCommand(() => Application.Current.Shutdown());
    }

    private void Navigate(string viewName)
    {
        _regionManager.RequestNavigate("ContentRegion", viewName);
    }
}
