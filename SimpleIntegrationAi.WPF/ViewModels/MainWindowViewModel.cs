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
    private readonly AsyncCommand _loadFileCommand;
    private readonly IResponseParser _responseParser;
    private readonly IYandexGpt _yandexGpt;
    private readonly AsyncCommand _addRelatedEntitiesCommand;
    private readonly Command _deleteEntityCommand;
    private readonly string foledrId = "b1gphb1c693npe94nmrv";

    private readonly string iamtoken =
        "t1.9euelZqWnZWKm4qZl5bKk4qMy5jIku3rnpWam46WzZKSlMaVjsycjZCZksjl8_cKH3RM-e9xQHwh_d3z90pNcUz573FAfCH9zef1656VmprOipLJxpSSlciOlMrMjMyN7_zF656VmprOipLJxpSSlciOlMrMjMyN.03noIyK_w1so2oFjDdKxohFTXnNoYBtLS_j70h7P4nt8tPukrTKjqKGZWADqf50gquIkxonrWzWxqsGUvIDYDQ";

    private ObservableCollection<MessageEntity> _items;

    public MainWindowViewModel(IYandexGpt yandexGpt, IResponseParser responseParser)
    {
        _addEntityCommand = new Command(AddEntity);
        _loadFileCommand = new AsyncCommand(LoadFile);
        _yandexGpt = yandexGpt;
        _items = new ObservableCollection<MessageEntity>();
        _responseParser = responseParser;
        _addRelatedEntitiesCommand = new AsyncCommand(AddRelatedEntites);
        _deleteEntityCommand = new Command(DeleteEntity);
    }

    public ICommand AddEntityCommand => _addEntityCommand;
    public ICommand LoadFileCommand => _loadFileCommand;
    public ICommand AddRelatedEntitiesCommand => _addRelatedEntitiesCommand;
    public ICommand DeleteEntityCommand => _deleteEntityCommand;
    private MessageEntity _selectedEntity;

    public MessageEntity SelectedEntity
    {
        get => _selectedEntity;
        set
        {
            _selectedEntity = value;
            SetField(ref _selectedEntity, value);
        }
    }
    public ObservableCollection<MessageEntity> Items
    {
        get => _items;
        set => SetField(ref _items, value);
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
                Items = [messageViewModel];
            }

            else if (fileExtension == ".txt")
            {
                var messageViewModel = new MessageEntity(File.ReadAllText(filePath));
                Items = [messageViewModel];
            }
        }

        var result = MessageBox.Show("Хотите чтобы ИИ обработал данные файла?", "Повторить?", MessageBoxButton.YesNo);

        if (result == MessageBoxResult.Yes) await InitializeAsync();
    }

    public async Task InitializeAsync()
    {
        var result = string.Join(" ", Items.Select(i => i.Message));

        var messageCollection =
            await _yandexGpt.Request($"{result} Выбери из списка сущности и выведи их без описания.", iamtoken,
                foledrId);

        var parsedMessage = _responseParser.GetMessageAsync(messageCollection);

        Items.Clear();

        foreach (var msg in parsedMessage)
            Items.Add(new MessageEntity(msg));
    }

    private void AddEntity()
    {
        var addEntityViewModel = new AddEntityDialogViewModel();
        var addEntityView = new AddEntityDialog
        {
            DataContext = addEntityViewModel
        };

        var dialogResult = addEntityView.ShowDialog();
        if (dialogResult == true) Items.Add(new MessageEntity(addEntityViewModel.EntityName));
    }

    public async Task AddRelatedEntites()
    {
        var result = string.Join(" ", Items.Select(i => i.Message));
        var addEntityViewModel = new AddEntityDialogViewModel();
        var addEntityView = new AddEntityDialog
        {
            DataContext = addEntityViewModel
        };

        var dialogResult = addEntityView.ShowDialog();
        if (dialogResult == true)
        {
            var messageCollection =
                await _yandexGpt.Request($"Дай мне {addEntityViewModel.EntityName} сущностей, расширяющих мой список"
                    , iamtoken, foledrId);

            var parsedMessage = _responseParser.GetMessageAsync(messageCollection);

            foreach (var msg in parsedMessage)
                Items.Add(new MessageEntity(msg));
        }
    }

    private void DeleteEntity()
    {
        if (SelectedEntity != null)
        {
            Items.Remove(SelectedEntity);
        }
    }
    private bool CanDeleteEntity()
    {
        return SelectedEntity != null;
    }
}