using Prism.Mvvm;
using Prism.Navigation.Regions;

namespace HeatingOilTracker.ViewModels;

public class ReferenceViewModel : BindableBase, INavigationAware
{
    public void OnNavigatedTo(NavigationContext navigationContext) { }
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }
}
