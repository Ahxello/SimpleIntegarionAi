using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using AiTestLibrary.Interfaces;
using GemBox.Document;
using Microsoft.Win32;
using SimpleIntegrationAi.Domain.Models;
using SimpleIntegrationAi.Domain.Services;
using SimpleIntegrationAi.WPF.Commands;
using SimpleIntegrationAi.WPF.Dialogs;

namespace SimpleIntegrationAi.WPF.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly Command _addEntityCommand;

    private readonly AsyncCommand _addRelatedEntitiesCommand;

    private readonly Command _deleteEntityCommand;

    private readonly AsyncCommand _loadFileCommand;

    private readonly AsyncCommand _addPropertiesCommand;

    private readonly IResponseParser _responseParser;

    private readonly IYandexGpt _yandexGpt;

    private readonly Command _addEntityPropertyCommand;

    private readonly Command _moveSelectedEntityCommand;

    private readonly Command _moveAllEntitiesCommand;

    private readonly string foledrId = "b1gphb1c693npe94nmrv";

    private readonly string iamtoken =
        "t1.9euelZqcmcbHysqXk5rJjYrKj5aekO3rnpWam46WzZKSlMaVjsycjZCZksjl8_cbfW5M-e9iJD1n_N3z91srbEz572IkPWf8zef1656VmpHPzImTk5bOi5fMxpCNl8mO7_zF656VmpHPzImTk5bOi5fMxpCNl8mO.qj0-znv45zKOSoZwadIodEDn47PjuF_F-OPRl1Lsq24tZopMt7HU3JPu33owyf-r9tUOpC0sRTdUDf0pa61mCQ";

    private ObservableCollection<MessageEntity> _temporaryEntities;
    private ObservableCollection<MessageEntity> _permanentEntities;
    private MessageEntity _selectedTemporaryEntity;
    private MessageEntity _selectedPermanentEntity;

    public MainWindowViewModel(IYandexGpt yandexGpt, IResponseParser responseParser)
    {
        _addEntityCommand = new Command(AddEntity);

        _loadFileCommand = new AsyncCommand(LoadFile);

        _yandexGpt = yandexGpt;

        _temporaryEntities = new ObservableCollection<MessageEntity>();

        _permanentEntities = new ObservableCollection<MessageEntity>();

        _responseParser = responseParser;

        _addEntityPropertyCommand = new Command(AddProperty);

        _addRelatedEntitiesCommand = new AsyncCommand(AddRelatedEntites);

        _deleteEntityCommand = new Command(DeleteEntity);

        _addPropertiesCommand = new AsyncCommand(AddProperties);

        _moveSelectedEntityCommand = new Command(MoveSelectedEntity);

        _moveAllEntitiesCommand = new Command(MoveAllEntities);

    }

    public ICommand AddEntityCommand => _addEntityCommand;
    public ICommand LoadFileCommand => _loadFileCommand;
    public ICommand AddRelatedEntitiesCommand => _addRelatedEntitiesCommand;
    public ICommand DeleteEntityCommand => _deleteEntityCommand;
    public ICommand AddPropertiesCommand => _addPropertiesCommand;
    public ICommand AddEntityPropertyCommand => _addEntityPropertyCommand;
    public ICommand MoveSelectedEntityCommand => _moveSelectedEntityCommand;
    public ICommand MoveAllEntitiesCommand => _moveAllEntitiesCommand;
    public MessageEntity SelectedTemporaryEntity
    {
        get => _selectedTemporaryEntity;
        set
        {
            _selectedTemporaryEntity = value;
            SetField(ref _selectedTemporaryEntity, value);
        }
    }

    public ObservableCollection<MessageEntity> TemporaryEntities
    {
        get => _temporaryEntities;
        set => SetField(ref _temporaryEntities, value);
    }

    public MessageEntity SelectedPermanentEntity
    {
        get => _selectedPermanentEntity;
        set
        {
            _selectedPermanentEntity = value;
            SetField(ref _selectedPermanentEntity, value);
        }
    }
    public ObservableCollection<MessageEntity> PermanentEntities
    {
        get => _permanentEntities;
        set => SetField(ref _permanentEntities, value);
    }


    private async Task LoadFile()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Text Files (*.txt)|*.txt|Word Documents (*.docx)|*.docx|All Files (*.*)|*.*"
        };
        if (openFileDialog.ShowDialog() == true)
        {
            var filePath = openFileDialog.FileName;
            var fileExtension = Path.GetExtension(filePath);

            if (fileExtension == ".docx")
            {
                ComponentInfo.SetLicense("FREE-LIMITED-KEY");
                var document = DocumentModel.Load(filePath);
                var messageViewModel = new MessageEntity(document.Content.ToString());
                TemporaryEntities = [messageViewModel];
            }

            else if (fileExtension == ".txt")
            {
                var messageViewModel = new MessageEntity(File.ReadAllText(filePath));
                TemporaryEntities = [messageViewModel];
            }

            if (filePath != null)
            {
                var result = MessageBox.Show("Хотите чтобы ИИ обработал данные файла?", "Повторить?",
                    MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes) await InitializeAsync();
            }
        }
    }

    public async Task InitializeAsync()
    {
        var result = string.Join(" ", TemporaryEntities.Select(i => i.Message));

        var messageCollection =
            await _yandexGpt.Request($"{result} Выбери из списка сущности и выведи их без описания.", iamtoken,
                foledrId);

        var parsedMessage = _responseParser.GetMessageAsync(messageCollection);

        TemporaryEntities.Clear();

        foreach (var msg in parsedMessage)
            TemporaryEntities.Add(new MessageEntity(msg));
    }

    private void AddEntity()
    {
        var addEntityViewModel = new AddEntityDialogViewModel();
        var addEntityView = new AddEntityDialog
        {
            DataContext = addEntityViewModel
        };

        var dialogResult = addEntityView.ShowDialog();
        if (dialogResult == true) TemporaryEntities.Add(new MessageEntity(addEntityViewModel.EntityName));
    }

    public async Task AddRelatedEntites()
    {
        var result = string.Join(" ", TemporaryEntities.Select(i => i.Message));
        var addRelatedEntityViewModel = new AddRelatedEntityDialogViewModel();
        var addRelatedEntityView = new AddRelatedEntityDialog
        {
            DataContext = addRelatedEntityViewModel
        };

        var dialogResult = addRelatedEntityView.ShowDialog();
        if (dialogResult == true)
        {
            var messageCollection =
                await _yandexGpt.Request(
                    $"Дай мне {addRelatedEntityViewModel.EntityCount} сущностей, расширяющих мой список"
                    , iamtoken, foledrId);

            var parsedMessage = _responseParser.GetMessageAsync(messageCollection);

            foreach (var msg in parsedMessage)
                TemporaryEntities.Add(new MessageEntity(msg));
        }
    }

    public async Task AddProperties()
    {
        var result = string.Join(" ", TemporaryEntities.Select(i => i.Message));
        var messageCollection =
            await _yandexGpt.Request($"{result} " +
                                     $"Опиши свойства для сущности из списка " +
                                     $"для дальнейшего внесения данных в БД " +
                                     $"с их типами данных", iamtoken,
                foledrId);
        var parsedMessage = _responseParser.GetMessageAsync(messageCollection);

        TemporaryEntities.Clear();

        foreach (var msg in parsedMessage)
            TemporaryEntities.Add(new MessageEntity(msg));
    }
    private void AddProperty()
    {
        if(SelectedTemporaryEntity != null)
            SelectedTemporaryEntity.Properties.Add(new DynamicProperty { Name = "New Property", Value = "Value" });
        if (SelectedPermanentEntity != null)
            SelectedPermanentEntity.Properties.Add(new DynamicProperty { Name = "New Property", Value = "Value" });
    }

    private void DeleteEntity()
    {
        if (SelectedTemporaryEntity != null) TemporaryEntities.Remove(SelectedTemporaryEntity);
        if (SelectedPermanentEntity != null) PermanentEntities.Remove(SelectedPermanentEntity);
    }

    private void MoveSelectedEntity()
    {
        if (SelectedTemporaryEntity != null)
        {
            PermanentEntities.Add(SelectedTemporaryEntity);
            TemporaryEntities.Remove(SelectedTemporaryEntity);
        }
    }

    private void MoveAllEntities()
    {
        foreach (var entity in TemporaryEntities.ToList())
        {
            PermanentEntities.Add(entity);
        }
        TemporaryEntities.Clear();
    }

}