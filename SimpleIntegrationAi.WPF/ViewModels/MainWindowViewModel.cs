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
    private readonly IGeminiGpt _geminiGpt;

    private readonly IYandexGpt _yandexGpt;

    private readonly Command _moveSelectedEntityCommand;

    private readonly Command _moveAllEntitiesCommand;

    private readonly string foledrId = "b1gphb1c693npe94nmrv";

    private readonly string iamtoken =
        "t1.9euelZqYnZ3ImpyelJ6ayZKal5aRz-3rnpWam46WzZKSlMaVjsycjZCZksjl8_dna0pM-e9kdS9d_N3z9ycaSEz572R1L138zef1656Vmp6WxpXNks3Il5GJnsiRkpiY7_zF656Vmp6WxpXNks3Il5GJnsiRkpiY.Xuyhnc6xzrdkOPKi54WTlnMny4T41YI7FVYrOueiPFfjxVuv7fBYLrSJFanKJYAHvq7PrEXYAsnSe3J2iw7vDQ";

    private ObservableCollection<Product> _temporaryEntities;
    private ObservableCollection<Product> _permanentEntities;
    private Product _selectedTemporaryEntity;
    private Product _selectedPermanentEntity;
    private readonly AsyncCommand _loadApiResponseCommand;
    private readonly AsyncCommand _addRelatedEntityCommand;

    public MainWindowViewModel(IYandexGpt yandexGpt, IResponseParser responseParser, IGeminiGpt geminiGpt)
    {


        _yandexGpt = yandexGpt;

        _temporaryEntities = new ObservableCollection<Product>();

        _permanentEntities = new ObservableCollection<Product>();

        _responseParser = responseParser;

        _geminiGpt = geminiGpt;

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
            await _geminiGpt.Request($"{result} Привет, сейчас я отправлю тебе список. Тебе нужно будет его разделить на справочные сущности и обычные сущности, также указать связи между сущностями для реляционной базы данных. Справочная сущность будет иметь только поля Id и Name.\r\nВот примерная структура твоего ответа\r\nEntities: \r\nEntity 1\r\n\tFields\r\nEntity 2\r\n\tFields\r\nEntity 3\r\n\tFields\r\n\r\nRelationships: \r\n Entity 1 - Entity 2: Relationship", "AIzaSyDQFWBEW9yx2wduHg_fE38oOeEgrFOPtI8");
        JObject json = JObject.Parse(messageCollection);
        string text = json["result"]["alternatives"][0]["message"]["text"].ToString();


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
                string[] lines = File.ReadAllLines(filePath);
                var entities = new Dictionary<string, List<string>>();
                var data = new Dictionary<string, List<string>>();
                var relationships = new List<(string from, string to, string type)>();
                ResponseParser parser = new ResponseParser();
                parser.Parse(lines, entities, data, relationships);
                var messageCollection =
                    await _geminiGpt.Request($" Привет, сейчас я отправлю тебе список. Тебе нужно будет его разделить на справочные сущности и обычные сущности, также указать связи между сущностями для реляционной базы данных. Справочная сущность будет иметь только поля Id и Name.\r\nВот примерная структура твоего ответа\r\nEntities: \r\nEntity 1\r\n\tFields\r\nEntity 2\r\n\tFields\r\nEntity 3\r\n\tFields\r\n\r\nRelationships: \r\n Entity 1 - Entity 2: Relationship", "AIzaSyDQFWBEW9yx2wduHg_fE38oOeEgrFOPtI8");
                
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