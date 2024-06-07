using System.ComponentModel;
using System.Windows.Input;
using SimpleIntegrationAi.WPF.ViewModels;

namespace SimpleIntegrationAi.WPF.State.Navigators;

public enum ViewType
{
    Home,
    About
}


public interface INavigator 
{
    ViewModelBase CurrentViewModel { get; set; }
    ICommand UpdateCurrentViewModelCommand { get; }
}