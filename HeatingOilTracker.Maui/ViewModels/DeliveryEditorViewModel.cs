using HeatingOilTracker.Core.Interfaces;
using HeatingOilTracker.Core.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace HeatingOilTracker.Maui.ViewModels;

[QueryProperty(nameof(DeliveryId), "deliveryId")]
public class DeliveryEditorViewModel : INotifyPropertyChanged
{
    private readonly IDataService _dataService;

    private string _deliveryId = string.Empty;
    private OilDelivery? _existingDelivery;

    private DateTime _date = DateTime.Today;
    private string _gallonsText = string.Empty;
    private string _pricePerGallonText = string.Empty;
    private string _notes = string.Empty;
    private bool _filledToCapacity = true;
    private string _title = "Add Delivery";
    private string _totalCostDisplay = "$0.00";

    public string DeliveryId
    {
        get => _deliveryId;
        set
        {
            _deliveryId = value;
            if (!string.IsNullOrEmpty(value))
                _ = LoadExistingAsync(Guid.Parse(value));
        }
    }

    public string Title { get => _title; set => SetProperty(ref _title, value); }
    public DateTime Date { get => _date; set { SetProperty(ref _date, value); UpdateTotalCost(); } }
    public bool FilledToCapacity { get => _filledToCapacity; set => SetProperty(ref _filledToCapacity, value); }
    public string Notes { get => _notes; set => SetProperty(ref _notes, value); }
    public string TotalCostDisplay { get => _totalCostDisplay; set => SetProperty(ref _totalCostDisplay, value); }

    public string GallonsText
    {
        get => _gallonsText;
        set
        {
            SetProperty(ref _gallonsText, value);
            UpdateTotalCost();
            SaveCommand.ChangeCanExecute();
        }
    }

    public string PricePerGallonText
    {
        get => _pricePerGallonText;
        set
        {
            SetProperty(ref _pricePerGallonText, value);
            UpdateTotalCost();
            SaveCommand.ChangeCanExecute();
        }
    }

    public Command SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public DeliveryEditorViewModel(IDataService dataService)
    {
        _dataService = dataService;
        SaveCommand = new Command(async () => await SaveAsync(), CanSave);
        CancelCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    private async Task LoadExistingAsync(Guid id)
    {
        var deliveries = await _dataService.GetDeliveriesAsync();
        _existingDelivery = deliveries.FirstOrDefault(d => d.Id == id);
        if (_existingDelivery == null) return;

        Title = "Edit Delivery";
        Date = _existingDelivery.Date;
        GallonsText = _existingDelivery.Gallons.ToString("F1");
        PricePerGallonText = _existingDelivery.PricePerGallon.ToString("F3");
        Notes = _existingDelivery.Notes;
        FilledToCapacity = _existingDelivery.FilledToCapacity;
    }

    private void UpdateTotalCost()
    {
        if (decimal.TryParse(GallonsText, out var g) && decimal.TryParse(PricePerGallonText, out var p))
            TotalCostDisplay = $"{Math.Round(g * p, 2):C2}";
        else
            TotalCostDisplay = "$0.00";
    }

    private bool CanSave()
    {
        return decimal.TryParse(GallonsText, out var g) && g > 0 &&
               decimal.TryParse(PricePerGallonText, out var p) && p > 0;
    }

    private async Task SaveAsync()
    {
        if (!decimal.TryParse(GallonsText, out var gallons) ||
            !decimal.TryParse(PricePerGallonText, out var price)) return;

        if (_existingDelivery != null)
        {
            _existingDelivery.Date = Date;
            _existingDelivery.Gallons = gallons;
            _existingDelivery.PricePerGallon = price;
            _existingDelivery.Notes = Notes;
            _existingDelivery.FilledToCapacity = FilledToCapacity;
            await _dataService.UpdateDeliveryAsync(_existingDelivery);
        }
        else
        {
            await _dataService.AddDeliveryAsync(new OilDelivery
            {
                Id = Guid.NewGuid(),
                Date = Date,
                Gallons = gallons,
                PricePerGallon = price,
                Notes = Notes,
                FilledToCapacity = FilledToCapacity
            });
        }

        await Shell.Current.GoToAsync("..");
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