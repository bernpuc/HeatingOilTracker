using HeatingOilTracker.Core.Interfaces;
using HeatingOilTracker.Core.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace HeatingOilTracker.Maui.ViewModels;

public class DeliveriesViewModel : INotifyPropertyChanged
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
        set
        {
            SetProperty(ref _selectedDelivery, value);
            OnPropertyChanged(nameof(HasSelection));
        }
    }

    public bool HasDeliveries { get => _hasDeliveries; set => SetProperty(ref _hasDeliveries, value); }
    public bool HasSelection => SelectedDelivery != null;
    public string TotalDeliveriesText { get => _totalDeliveriesText; set => SetProperty(ref _totalDeliveriesText, value); }
    public string AveragePriceText { get => _averagePriceText; set => SetProperty(ref _averagePriceText, value); }
    public string AverageBurnRateText { get => _averageBurnRateText; set => SetProperty(ref _averageBurnRateText, value); }
    public string AverageKFactorText { get => _averageKFactorText; set => SetProperty(ref _averageKFactorText, value); }

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SelectDeliveryCommand { get; }

    public DeliveriesViewModel(IDataService dataService, ICsvImportService csvImportService, IWeatherService weatherService)
    {
        _dataService = dataService;
        _csvImportService = csvImportService;
        _weatherService = weatherService;

        AddCommand = new Command(async () => await NavigateToAddAsync());
        EditCommand = new Command(async () => await NavigateToEditAsync(), () => SelectedDelivery != null);
        DeleteCommand = new Command(async () => await DeleteDeliveryAsync(), () => SelectedDelivery != null);
        ImportCommand = new Command(async () => await ImportCsvAsync());
        ExportCommand = new Command(async () => await ExportCsvAsync(), () => HasDeliveries);
        RefreshCommand = new Command(async () => await LoadDeliveriesAsync());
        SelectDeliveryCommand = new Command<DeliveryDisplayItem>(item => SelectedDelivery = item);

        _ = LoadDeliveriesAsync();
    }

    public async Task LoadDeliveriesAsync()
    {
        try
        {
            var deliveries = await _dataService.GetDeliveriesAsync();
            var weatherData = await _dataService.GetWeatherHistoryAsync();

            var sorted = deliveries.OrderBy(d => d.Date).ToList();
            var displayItems = new List<DeliveryDisplayItem>();

            for (int i = 0; i < sorted.Count; i++)
            {
                var previous = i > 0 ? sorted[i - 1] : null;
                decimal? hdd = null;
                if (previous != null && weatherData.Count > 0)
                {
                    hdd = _weatherService.CalculateHDD(weatherData, previous.Date, sorted[i].Date);
                    if (hdd == 0) hdd = null;
                }
                displayItems.Add(new DeliveryDisplayItem(sorted[i], previous, hdd));
            }

            displayItems.Reverse();
            Deliveries = new ObservableCollection<DeliveryDisplayItem>(displayItems);
            HasDeliveries = Deliveries.Count > 0;
            UpdateSummary(displayItems);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error loading deliveries: {ex.Message}", "OK");
        }
    }

    private void UpdateSummary(List<DeliveryDisplayItem> items)
    {
        if (items.Count == 0)
        {
            TotalDeliveriesText = "No deliveries";
            AveragePriceText = "--";
            AverageBurnRateText = "--";
            AverageKFactorText = "--";
            return;
        }

        TotalDeliveriesText = $"{items.Count} deliveries";
        AveragePriceText = $"Avg $/gal: {items.Average(d => d.PricePerGallon):F3}";

        var burnRates = items.Where(d => d.GallonsPerDay.HasValue).Select(d => d.GallonsPerDay!.Value).ToList();
        AverageBurnRateText = burnRates.Count > 0 ? $"Avg burn: {burnRates.Average():F1} gal/day" : "Avg burn: --";

        var kFactors = items.Where(d => d.KFactor.HasValue).Select(d => d.KFactor!.Value).ToList();
        AverageKFactorText = kFactors.Count > 0 ? $"Avg K: {kFactors.Average():F3}" : "Avg K: --";
    }

    private async Task NavigateToAddAsync()
    {
        await Shell.Current.GoToAsync("DeliveryEditorPage");
    }

    private async Task NavigateToEditAsync()
    {
        if (SelectedDelivery == null) return;
        var id = SelectedDelivery.Id.ToString();
        await Shell.Current.GoToAsync($"DeliveryEditorPage?deliveryId={id}");
    }

    private async Task DeleteDeliveryAsync()
    {
        if (SelectedDelivery == null) return;

        var confirmed = await Shell.Current.DisplayAlert(
            "Confirm Delete",
            $"Delete delivery from {SelectedDelivery.Date:d}?\n{SelectedDelivery.Gallons:F1} gallons @ {SelectedDelivery.PricePerGallon:C3}/gal",
            "Delete", "Cancel");

        if (!confirmed) return;

        await _dataService.DeleteDeliveryAsync(SelectedDelivery.Id);
        SelectedDelivery = null;
        await LoadDeliveriesAsync();
    }

    private async Task ImportCsvAsync()
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Select CSV file",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "text/csv", "text/comma-separated-values" } },
                    { DevicePlatform.WinUI, new[] { ".csv" } }
                })
            });

            if (result == null) return;

            var existing = await _dataService.GetDeliveriesAsync();
            var importResult = await _csvImportService.ImportFromCsvAsync(result.FullPath, existing);

            if (importResult.ImportedDeliveries.Count > 0)
            {
                var data = await _dataService.LoadAsync();
                data.Deliveries.AddRange(importResult.ImportedDeliveries);
                await _dataService.SaveAsync(data);
            }

            await LoadDeliveriesAsync();

            var message = $"Import complete: {importResult.Summary}";
            if (importResult.Errors.Count > 0)
                message += $"\n\n{importResult.Errors.Count} errors occurred.";

            await Shell.Current.DisplayAlert("CSV Import", message, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Import failed: {ex.Message}", "OK");
        }
    }

    private async Task ExportCsvAsync()
    {
        try
        {
            var deliveries = await _dataService.GetDeliveriesAsync();
            var fileName = $"OilDeliveries_{DateTime.Now:yyyyMMdd}.csv";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            using var writer = new StreamWriter(filePath);
            await writer.WriteLineAsync("Date,Gallons,Price Per Gallon,Total Cost,Notes");
            foreach (var d in deliveries.OrderBy(d => d.Date))
                await writer.WriteLineAsync($"{d.Date:yyyy-MM-dd},{d.Gallons:F1},{d.PricePerGallon:F3},{d.Gallons * d.PricePerGallon:F2},{d.Notes}");

            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Export Deliveries CSV",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Export failed: {ex.Message}", "OK");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}