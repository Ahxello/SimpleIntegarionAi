using System.Windows.Input;
using SimpleIntegrationAi.WPF.State.Navigators;
using SimpleIntegrationAi.WPF.ViewModels;

namespace SimpleIntegrationAi.WPF.Commands;

public class UpdateCurrentViewModelCommand : ICommand
{
    private INavigator _navigator;

    public UpdateCurrentViewModelCommand(INavigator navigator)
    {
        _navigator = navigator;
    }
    public bool CanExecute(object? parameter)
    {
        return true;
    }

    public void Execute(object? parameter)
    {
        if (parameter is ViewType)
        {
            ViewType viewType = (ViewType)parameter;
            switch (viewType)
            {
                case ViewType.Home:
                    _navigator.CurrentViewModel = new MainWindowViewModel();
                    break;
                case ViewType.About:
                    _navigator.CurrentViewModel = new MainWindowViewModel();
                    break;
                default:
                    break;
            }
        }
    }

    public event EventHandler CanExecuteChanged;
    
}