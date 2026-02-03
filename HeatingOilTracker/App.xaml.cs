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
        containerRegistry.Register<IWeatherService, WeatherService>();
        containerRegistry.Register<ITankEstimatorService, TankEstimatorService>();
        containerRegistry.Register<IReportService, ReportService>();

        // Views for navigation
        containerRegistry.RegisterForNavigation<DashboardView, DashboardViewModel>();
        containerRegistry.RegisterForNavigation<DeliveriesView, DeliveriesViewModel>();
        containerRegistry.RegisterForNavigation<ChartsView, ChartsViewModel>();
        containerRegistry.RegisterForNavigation<ReportsView, ReportsViewModel>();
        containerRegistry.RegisterForNavigation<SettingsView, SettingsViewModel>();
        containerRegistry.RegisterForNavigation<ReferenceView, ReferenceViewModel>();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        var regionManager = Container.Resolve<IRegionManager>();
        regionManager.RequestNavigate("ContentRegion", "DashboardView");
    }
}
