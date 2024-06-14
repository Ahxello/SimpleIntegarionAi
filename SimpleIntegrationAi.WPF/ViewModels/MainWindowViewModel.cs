using System.Collections.ObjectModel;
using System.IO;
using System.Reflection.Metadata;
using System.Windows;
using System.Windows.Input;
using AiTestLibrary.Interfaces;
using GemBox.Document;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using SimpleIntegrationAi.Domain.Models;
using SimpleIntegrationAi.Domain.Services;
using SimpleIntegrationAi.WPF.Commands;
using SimpleIntegrationAi.WPF.Dialogs;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

namespace SimpleIntegrationAi.WPF.ViewModels;

public class MainWindowViewModel : ViewModelBase
{

    private readonly Command _deleteEntityCommand;

    private readonly AsyncCommand _loadFileCommand;

    private readonly IResponseParser _responseParser;

    private readonly IYandexGpt _yandexGpt;

    private readonly Command _moveSelectedEntityCommand;

    private readonly Command _moveAllEntitiesCommand;

    private readonly string foledrId = "b1gphb1c693npe94nmrv";

    private readonly string iamtoken =
        "t1.9euelZqbismZmMeSjpiQlM6JyYqPk-3rnpWam46WzZKSlMaVjsycjZCZksjl8_c3R09M-e9DIl1t_t3z93d1TEz570MiXW3-zef1656VmsrOyJaSks6TjpGci47Ky46L7_zF656VmsrOyJaSks6TjpGci47Ky46L.nrA5RArkkBdkJFtk9ZCHZRN8yhZDFxScvQ68Og_KVCDAffC-8riOnPnEkeDodnnmTxE25V0vI-zKAliwQCjTBA";

    private ObservableCollection<Product> _temporaryEntities;
    private ObservableCollection<Product> _permanentEntities;
    private Product _selectedTemporaryEntity;
    private Product _selectedPermanentEntity;
    private readonly AsyncCommand _loadApiResponseCommand;
    private readonly AsyncCommand _addRelatedEntityCommand;

    public MainWindowViewModel(IYandexGpt yandexGpt, IResponseParser responseParser)
    {


        _yandexGpt = yandexGpt;

        _temporaryEntities = new ObservableCollection<Product>();

        _permanentEntities = new ObservableCollection<Product>();

        _responseParser = responseParser;

        _deleteEntityCommand = new Command(DeleteEntity);

        _moveSelectedEntityCommand = new Command(MoveSelectedEntity);

        _moveAllEntitiesCommand = new Command(MoveAllEntities);

        _loadApiResponseCommand = new AsyncCommand(LoadApiResponse);

        _addRelatedEntityCommand = new AsyncCommand(AddRelatedEntities);

    }

    public ICommand LoadFileCommand => _loadFileCommand;
    public ICommand MoveSelectedEntityCommand => _moveSelectedEntityCommand;
    public ICommand MoveAllEntitiesCommand => _moveAllEntitiesCommand;
    public ICommand AddRelatedEntityCommand => _addRelatedEntityCommand;
    public ICommand LoadApiResponseCommand => _loadApiResponseCommand;
    public Product SelectedTemporaryEntity
    {
        get => _selectedTemporaryEntity;
        set
        {
            _selectedTemporaryEntity = value;
            SetField(ref _selectedTemporaryEntity, value);
        }
    }

    public ObservableCollection<Product> TemporaryEntities
    {
        get => _temporaryEntities;
        set => SetField(ref _temporaryEntities, value);
    }

    public Product SelectedPermanentEntity
    {
        get => _selectedPermanentEntity;
        set
        {
            _selectedPermanentEntity = value;
            SetField(ref _selectedPermanentEntity, value);
        }
    }
    public ObservableCollection<Product> PermanentEntities
    {
        get => _permanentEntities;
        set => SetField(ref _permanentEntities, value);
    }


   

    public async Task InitializeAsync()
    {
        var result = string.Join(" ", TemporaryEntities.Select(i => i.Message));

        var messageCollection =
            await _yandexGpt.Request($"{result} Привет, сейчас я отправлю тебе список. Тебе нужно будет его разделить на справочные сущности и обычные сущности, также указать связи между сущностями для реляционной базы данных. Справочная сущность будет иметь только поля Id и Name.\r\nВот примерная структура твоего ответа\r\nEntities: \r\nEntity 1\r\n\tFields\r\nEntity 2\r\n\tFields\r\nEntity 3\r\n\tFields\r\n\r\nRelationships: \r\n Entity 1 - Entity 2: Relationship\r\n\r\nДетализированный анализ \r\nEntity 1\r\n Data\r\nEntity 2\r\n Data\r\nEntity 3 \r\n Data", iamtoken,
                foledrId);
        JObject json = JObject.Parse(messageCollection);
        string text = json["result"]["alternatives"][0]["message"]["text"].ToString();

        var apiResponse = new ApiResponse();

        apiResponse.Entities = _responseParser.ParseEntities(text);


        TemporaryEntities.Clear();
    }

    public async Task AddRelatedEntities()
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
                TemporaryEntities.Add(new Product(msg));
        }
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

    private async Task LoadApiResponse()
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
                var messageViewModel = new Product(document.Content.ToString());
                var msg = ApiResponseParser.ParseText(document.Content.ToString());

                TemporaryEntities = [messageViewModel];
            }

            else if (fileExtension == ".txt")
            {
                var messageViewModel = new Product(File.ReadAllText(filePath));
                var msg = ApiResponseParser.ParseText(File.ReadAllText(filePath));
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

}