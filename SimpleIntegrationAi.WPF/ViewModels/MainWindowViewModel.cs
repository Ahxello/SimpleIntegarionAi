using System.Collections.ObjectModel;

namespace SimpleIntegrationAi.WPF.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private ObservableCollection<string> _items;

    public ObservableCollection<string> Items
    {
        get => _items;
        set => SetField(ref _items, value);
    }
}