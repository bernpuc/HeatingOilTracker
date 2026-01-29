using HeatingOilTracker.Services;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System.Windows;

namespace HeatingOilTracker.ViewModels;

public class SettingsViewModel : BindableBase, INavigationAware
{
    private readonly IDataService _dataService;
    private decimal _tankCapacity;
    private string _dataFilePath = string.Empty;

    public decimal TankCapacity
    {
        get => _tankCapacity;
        set => SetProperty(ref _tankCapacity, value);
    }

    public string DataFilePath
    {
        get => _dataFilePath;
        set => SetProperty(ref _dataFilePath, value);
    }

    public DelegateCommand SaveCommand { get; }

    public SettingsViewModel(IDataService dataService)
    {
        _dataService = dataService;
        DataFilePath = _dataService.GetDataFilePath();
        SaveCommand = new DelegateCommand(async () => await SaveAsync());
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        TankCapacity = await _dataService.GetTankCapacityAsync();
    }

    private async Task SaveAsync()
    {
        if (TankCapacity <= 0)
        {
            MessageBox.Show("Tank capacity must be greater than zero.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await _dataService.SetTankCapacityAsync(TankCapacity);
        MessageBox.Show("Settings saved.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void OnNavigatedTo(NavigationContext navigationContext) => _ = LoadAsync();
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }
}
