using HeatingOilTracker.Models;
using Prism.Commands;
using Prism.Mvvm;

namespace HeatingOilTracker.ViewModels;

public class DeliveryEditorViewModel : BindableBase
{
    private DateTime _date;
    private decimal _gallons;
    private decimal _pricePerGallon;
    private string _notes = string.Empty;
    private string _title;
    private bool _filledToCapacity = true;

    public DateTime Date
    {
        get => _date;
        set
        {
            SetProperty(ref _date, value);
            RaisePropertyChanged(nameof(TotalCost));
        }
    }

    public decimal Gallons
    {
        get => _gallons;
        set
        {
            SetProperty(ref _gallons, value);
            RaisePropertyChanged(nameof(TotalCost));
            SaveCommand.RaiseCanExecuteChanged();
        }
    }

    public decimal PricePerGallon
    {
        get => _pricePerGallon;
        set
        {
            SetProperty(ref _pricePerGallon, value);
            RaisePropertyChanged(nameof(TotalCost));
            SaveCommand.RaiseCanExecuteChanged();
        }
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public bool FilledToCapacity
    {
        get => _filledToCapacity;
        set => SetProperty(ref _filledToCapacity, value);
    }

    public decimal TotalCost => Math.Round(Gallons * PricePerGallon, 2);

    public DelegateCommand<System.Windows.Window> SaveCommand { get; }
    public DelegateCommand<System.Windows.Window> CancelCommand { get; }

    public DeliveryEditorViewModel(OilDelivery? existing = null)
    {
        if (existing != null)
        {
            _title = "Edit Delivery";
            _date = existing.Date;
            _gallons = existing.Gallons;
            _pricePerGallon = existing.PricePerGallon;
            _notes = existing.Notes;
            _filledToCapacity = existing.FilledToCapacity;
        }
        else
        {
            _title = "Add Delivery";
            _date = DateTime.Today;
            _filledToCapacity = true;
        }

        SaveCommand = new DelegateCommand<System.Windows.Window>(Save, CanSave);
        CancelCommand = new DelegateCommand<System.Windows.Window>(Cancel);
    }

    private bool CanSave(System.Windows.Window? window)
    {
        return Gallons > 0 && PricePerGallon > 0;
    }

    private void Save(System.Windows.Window? window)
    {
        if (window != null)
        {
            window.DialogResult = true;
            window.Close();
        }
    }

    private void Cancel(System.Windows.Window? window)
    {
        if (window != null)
        {
            window.DialogResult = false;
            window.Close();
        }
    }
}
