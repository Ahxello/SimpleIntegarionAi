using SimpleIntegrationAi.WPF.State.Navigators;

namespace SimpleIntegrationAi.WPF.ViewModels.Factories;

public interface IViewModelAbstractFactory
{
    ViewModelBase CreateViewModel(ViewType viewType);
}
