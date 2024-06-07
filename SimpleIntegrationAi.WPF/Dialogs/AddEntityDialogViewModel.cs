using System.Windows;
using System.Windows.Input;
using SimpleIntegrationAi.WPF.Commands;
using SimpleIntegrationAi.WPF.ViewModels;

namespace SimpleIntegrationAi.WPF.Dialogs;

public class AddEntityDialogViewModel : ViewModelBase
{
    private string _entityName;

    public AddEntityDialogViewModel()
    {
        ConfirmCommand = new Command(Confirm);
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