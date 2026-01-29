using HeatingOilTracker.Services;
using HeatingOilTracker.ViewModels;
using HeatingOilTracker.Views;
using Prism.Ioc;
using Prism.Navigation.Regions;
using System.Windows;

namespace HeatingOilTracker;

public partial class App : PrismApplication
{
    protected override Window CreateShell()
    {
        var window = Container.Resolve<MainWindow>();
        var vm = Container.Resolve<MainWindowViewModel>();
        window.DataContext = vm;
        return window;
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Shell
        containerRegistry.Register<MainWindow>();
        containerRegistry.Register<MainWindowViewModel>();

        // Services
        containerRegistry.RegisterSingleton<IDataService, DataService>();
        containerRegistry.Register<ICsvImportService, CsvImportService>();

        // Views for navigation
        containerRegistry.RegisterForNavigation<DeliveriesView, DeliveriesViewModel>();
        containerRegistry.RegisterForNavigation<ChartsView, ChartsViewModel>();
        containerRegistry.RegisterForNavigation<SettingsView, SettingsViewModel>();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        var regionManager = Container.Resolve<IRegionManager>();
        regionManager.RequestNavigate("ContentRegion", "DeliveriesView");
    }
}
