using HeatingOilTracker.Core.Interfaces;
using HeatingOilTracker.Core.Services;
using HeatingOilTracker.Maui.Services;
using Microsoft.Extensions.DependencyInjection;
using HeatingOilTracker.Maui.ViewModels;
using HeatingOilTracker.Maui.Views;
using LiveChartsCore.SkiaSharpView.Maui;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace HeatingOilTracker.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .UseLiveCharts()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if ANDROID
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler<ScrollView, CustomScrollViewHandler>();
        });
#endif

        // Register services
        builder.Services.AddSingleton<ISyncService, GoogleDriveSyncService>();
        builder.Services.AddSingleton<IDataService>(sp =>
            new DataService(FileSystem.AppDataDirectory, sp.GetService<ISyncService>()));
        builder.Services.AddSingleton<IWeatherService, WeatherService>();
        builder.Services.AddSingleton<ITankEstimatorService, TankEstimatorService>();
        builder.Services.AddSingleton<IReportService, ReportService>();
        builder.Services.AddSingleton<ICsvImportService, CsvImportService>();
        builder.Services.AddSingleton<IEiaService>(_ =>
            new EiaService(() => Preferences.Get("eia_api_key", string.Empty)));

        // ViewModels
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<DeliveriesViewModel>();
        builder.Services.AddTransient<DeliveryEditorViewModel>(); 
        builder.Services.AddTransient<SettingsViewModel>(); 
        builder.Services.AddTransient<ReportsViewModel>(); 
        builder.Services.AddTransient<ChartsViewModel>();
        builder.Services.AddTransient<WeatherViewModel>();
        builder.Services.AddTransient<ReferenceViewModel>();
        builder.Services.AddTransient<PricesViewModel>();

        // Pages
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<DeliveriesPage>();
        builder.Services.AddTransient<DeliveryEditorPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<ReportsPage>();
        builder.Services.AddTransient<ChartsPage>();
        builder.Services.AddTransient<WeatherPage>();
        builder.Services.AddTransient<ReferencePage>();
        builder.Services.AddTransient<PricesPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Build the app first
        var mauiApp = builder.Build();

#if DEBUG
        var dataService = mauiApp.Services.GetRequiredService<IDataService>();
        Task.Run(async () => await DevDataSeeder.SeedIfEmptyAsync(dataService));
#endif

        return mauiApp;
    }
}