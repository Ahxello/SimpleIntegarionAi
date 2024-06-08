using SimpleIntegrationAi.WPF.Commands;
using SimpleIntegrationAi.WPF.ViewModels;
using System.Windows.Input;
using System.Windows;

namespace SimpleIntegrationAi.WPF.Dialogs;

public class AddRelatedEntityDialogViewModel : ViewModelBase
{
    private string _entityName;
    private int _entityCount;
    public AddRelatedEntityDialogViewModel()
    {
        ConfirmCommand = new Command(Confirm);
    }

    public int EntityCount
    {
        get => _entityCount;
        set => SetField(ref _entityCount, value);

    }
    public string EntityName
    {
        get => _entityName;
        set => SetField(ref _entityName, value);
    }
    

    public ICommand ConfirmCommand { get; }

    public bool? DialogResult { get; set; }

    private void Confirm()
    {
        var window = Application.Current.Windows
            .OfType<Window>()
            .SingleOrDefault(w => w.DataContext == this);

        if (window != null)
        {
            window.DialogResult = true;
            window.Close();
        }
    }
}