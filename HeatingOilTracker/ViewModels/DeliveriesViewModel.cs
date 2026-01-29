using HeatingOilTracker.Models;
using HeatingOilTracker.Services;
using HeatingOilTracker.Views;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace HeatingOilTracker.ViewModels;

public class DeliveriesViewModel : BindableBase, INavigationAware
{
    private readonly IDataService _dataService;
    private readonly ICsvImportService _csvImportService;
    private readonly IWeatherService _weatherService;

    private ObservableCollection<DeliveryDisplayItem> _deliveries = new();
    private DeliveryDisplayItem? _selectedDelivery;
    private bool _hasDeliveries;
    private string _totalDeliveriesText = string.Empty;
    private string _averagePriceText = string.Empty;
    private string _averageBurnRateText = string.Empty;
    private string _averageKFactorText = string.Empty;

    public ObservableCollection<DeliveryDisplayItem> Deliveries
    {
        get => _deliveries;
        set => SetProperty(ref _deliveries, value);
    }

    public DeliveryDisplayItem? SelectedDelivery
    {
        get => _selectedDelivery;
        set => SetProperty(ref _selectedDelivery, value);
    }

    public bool HasDeliveries
    {
        get => _hasDeliveries;
        set => SetProperty(ref _hasDeliveries, value);
    }

    public string TotalDeliveriesText
    {
        get => _totalDeliveriesText;
        set => SetProperty(ref _totalDeliveriesText, value);
    }

    public string AveragePriceText
    {
        get => _averagePriceText;
        set => SetProperty(ref _averagePriceText, value);
    }

    public string AverageBurnRateText
    {
        get => _averageBurnRateText;
        set => SetProperty(ref _averageBurnRateText, value);
    }

    public string AverageKFactorText
    {
        get => _averageKFactorText;
        set => SetProperty(ref _averageKFactorText, value);
    }

    public DelegateCommand AddCommand { get; }
    public DelegateCommand EditCommand { get; }
    public DelegateCommand DeleteCommand { get; }
    public DelegateCommand ImportCommand { get; }
    public DelegateCommand ExportCommand { get; }

    public DeliveriesViewModel(IDataService dataService, ICsvImportService csvImportService, IWeatherService weatherService)
    {
        _dataService = dataService;
        _csvImportService = csvImportService;
        _weatherService = weatherService;

        AddCommand = new DelegateCommand(AddDelivery);
        EditCommand = new DelegateCommand(EditDelivery, () => SelectedDelivery != null)
            .ObservesProperty(() => SelectedDelivery);
        DeleteCommand = new DelegateCommand(async () => await DeleteDeliveryAsync(), () => SelectedDelivery != null)
            .ObservesProperty(() => SelectedDelivery);
        ImportCommand = new DelegateCommand(async () => await ImportCsvAsync());
        ExportCommand = new DelegateCommand(async () => await ExportCsvAsync(), () => HasDeliveries)
            .ObservesProperty(() => HasDeliveries);

        _ = LoadDeliveriesAsync();
    }

    private async Task LoadDeliveriesAsync()
    {
        try
        {
            var deliveries = await _dataService.GetDeliveriesAsync();
            var weatherData = await _dataService.GetWeatherHistoryAsync();

            // Sort ascending by date to compute "previous" correctly
            var sorted = deliveries.OrderBy(d => d.Date).ToList();

            var displayItems = new List<DeliveryDisplayItem>();
            for (int i = 0; i < sorted.Count; i++)
            {
                var previous = i > 0 ? sorted[i - 1] : null;

                // Calculate HDD between previous delivery and this one
                decimal? hdd = null;
                if (previous != null && weatherData.Count > 0)
                {
                    hdd = _weatherService.CalculateHDD(weatherData, previous.Date, sorted[i].Date);
                    if (hdd == 0) hdd = null; // No data for this period
                }

                displayItems.Add(new DeliveryDisplayItem(sorted[i], previous, hdd));
            }

            // Show descending in the grid
            displayItems.Reverse();
            Deliveries = new ObservableCollection<DeliveryDisplayItem>(displayItems);

            HasDeliveries = Deliveries.Count > 0;
            UpdateSummary(displayItems);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading deliveries: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateSummary(List<DeliveryDisplayItem> displayItems)
    {
        if (displayItems.Count == 0)
        {
            TotalDeliveriesText = "No deliveries";
            AveragePriceText = "--";
            AverageBurnRateText = "--";
            AverageKFactorText = "--";
            return;
        }

        TotalDeliveriesText = $"{displayItems.Count} deliveries";

        var avgPrice = displayItems.Average(d => d.PricePerGallon);
        AveragePriceText = $"Avg $/gal: {avgPrice:F3}";

        // Compute average burn rate from items that have a previous delivery
        var burnRates = displayItems
            .Where(d => d.GallonsPerDay.HasValue)
            .Select(d => d.GallonsPerDay!.Value)
            .ToList();

        AverageBurnRateText = burnRates.Count > 0
            ? $"Avg burn: {burnRates.Average():F1} gal/day"
            : "Avg burn: --";

        // Compute average K-Factor from items that have HDD data
        var kFactors = displayItems
            .Where(d => d.KFactor.HasValue)
            .Select(d => d.KFactor!.Value)
            .ToList();

        AverageKFactorText = kFactors.Count > 0
            ? $"Avg K: {kFactors.Average():F3} gal/HDD"
            : "Avg K: --";
    }

    private void AddDelivery()
    {
        var vm = new DeliveryEditorViewModel();
        var window = new DeliveryEditorWindow { DataContext = vm };
        window.Owner = Application.Current.MainWindow;

        if (window.ShowDialog() == true)
        {
            var delivery = new OilDelivery
            {
                Date = vm.Date,
                Gallons = vm.Gallons,
                PricePerGallon = vm.PricePerGallon,
                Notes = vm.Notes
            };

            _ = SaveAndReloadAsync(async () => await _dataService.AddDeliveryAsync(delivery));
        }
    }

    private void EditDelivery()
    {
        if (SelectedDelivery == null) return;

        var existing = SelectedDelivery.Delivery;
        var vm = new DeliveryEditorViewModel(existing);
        var window = new DeliveryEditorWindow { DataContext = vm };
        window.Owner = Application.Current.MainWindow;

        if (window.ShowDialog() == true)
        {
            existing.Date = vm.Date;
            existing.Gallons = vm.Gallons;
            existing.PricePerGallon = vm.PricePerGallon;
            existing.Notes = vm.Notes;

            _ = SaveAndReloadAsync(async () => await _dataService.UpdateDeliveryAsync(existing));
        }
    }

    private async Task DeleteDeliveryAsync()
    {
        try
        {
            if (SelectedDelivery == null) return;

            var result = MessageBox.Show(
                $"Delete delivery from {SelectedDelivery.Date:d}?\n\n" +
                $"{SelectedDelivery.Gallons:F1} gallons @ {SelectedDelivery.PricePerGallon:C3}/gal",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await _dataService.DeleteDeliveryAsync(SelectedDelivery.Id);
                await LoadDeliveriesAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting delivery: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ImportCsvAsync()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Import Deliveries from CSV"
            };

            if (dialog.ShowDialog() != true) return;

            var existing = await _dataService.GetDeliveriesAsync();
            var importResult = await _csvImportService.ImportFromCsvAsync(dialog.FileName, existing);

            if (importResult.ImportedDeliveries.Count > 0)
            {
                var data = await _dataService.LoadAsync();
                data.Deliveries.AddRange(importResult.ImportedDeliveries);
                await _dataService.SaveAsync(data);
            }

            await LoadDeliveriesAsync();

            var message = $"Import complete: {importResult.Summary}";
            if (importResult.Errors.Count > 0)
            {
                message += "\n\nErrors:\n" + string.Join("\n", importResult.Errors.Take(10));
                if (importResult.Errors.Count > 10)
                    message += $"\n... and {importResult.Errors.Count - 10} more";
            }

            MessageBox.Show(message, "CSV Import",
                MessageBoxButton.OK,
                importResult.Errors.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Import failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ExportCsvAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            DefaultExt = "csv",
            FileName = $"OilDeliveries_{DateTime.Now:yyyyMMdd}.csv"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var deliveries = await _dataService.GetDeliveriesAsync();

            using var writer = new StreamWriter(dialog.FileName);
            using var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);

            csv.WriteField("Date");
            csv.WriteField("Gallons");
            csv.WriteField("Price Per Gallon");
            csv.WriteField("Total Cost");
            csv.WriteField("Notes");
            await csv.NextRecordAsync();

            foreach (var d in deliveries.OrderBy(d => d.Date))
            {
                csv.WriteField(d.Date.ToString("yyyy-MM-dd"));
                csv.WriteField(d.Gallons.ToString("F1"));
                csv.WriteField(d.PricePerGallon.ToString("F3"));
                csv.WriteField((d.Gallons * d.PricePerGallon).ToString("F2"));
                csv.WriteField(d.Notes);
                await csv.NextRecordAsync();
            }

            MessageBox.Show($"Exported {deliveries.Count} deliveries to:\n{dialog.FileName}",
                "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task SaveAndReloadAsync(Func<Task> action)
    {
        await action();
        await LoadDeliveriesAsync();
    }

    public void OnNavigatedTo(NavigationContext navigationContext) => _ = LoadDeliveriesAsync();
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }
}
