using System.Collections.ObjectModel;
using System.Data.SqlClient;
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

    private readonly AsyncCommand _loadFileCommand;

    private readonly IResponseParser _responseParser;

    private readonly IGeminiGpt _geminiGpt;

    private readonly IYandexGpt _yandexGpt;

    private ObservableCollection<Entity> _databaseEntities;

    private readonly string foledrId = "b1gphb1c693npe94nmrv";

    private readonly string iamtoken =
        "t1.9euelZqYnZ3ImpyelJ6ayZKal5aRz-3rnpWam46WzZKSlMaVjsycjZCZksjl8_dna0pM-e9kdS9d_N3z9ycaSEz572R1L138zef1656Vmp6WxpXNks3Il5GJnsiRkpiY7_zF656Vmp6WxpXNks3Il5GJnsiRkpiY.Xuyhnc6xzrdkOPKi54WTlnMny4T41YI7FVYrOueiPFfjxVuv7fBYLrSJFanKJYAHvq7PrEXYAsnSe3J2iw7vDQ";

    string connectionString = "Server=localhost;Database=local_database;Trusted_Connection=True;";
    private ObservableCollection<Product> _temporaryEntities;
    private Product _selectedTemporaryEntity;
    private readonly AsyncCommand _loadApiResponseCommand;
    private readonly AsyncCommand _addRelatedEntityCommand;

    public MainWindowViewModel(IYandexGpt yandexGpt, IResponseParser responseParser, IGeminiGpt geminiGpt)
    {
        _yandexGpt = yandexGpt;

        _temporaryEntities = new ObservableCollection<Product>();
        
        _databaseEntities = new ObservableCollection<Entity>();

        _responseParser = responseParser;

        _geminiGpt = geminiGpt;

        _loadApiResponseCommand = new AsyncCommand(LoadApiResponse);

        _addRelatedEntityCommand = new AsyncCommand(AddRelatedEntities);

    }

    public ICommand LoadFileCommand => _loadFileCommand;
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

    public ObservableCollection<Entity> DatabaseEntities
    {
        get => _databaseEntities;
        set => SetField(ref _databaseEntities, value);
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

                TemporaryEntities = [messageViewModel];
            }

            else if (fileExtension == ".txt")
            {
                string[] lines = File.ReadAllLines(filePath);
                ResponseParser parser = new ResponseParser();
                var ents = parser.Parse(lines);
                CreateLocalDatabase();
                using (var dbService = new DatabaseService(connectionString))
                {
                    dbService.OpenConnection();

                    foreach (var entity in ents)
                    {
                        dbService.CreateTable(entity);
                        dbService.InsertData(entity);
                    }

                    dbService.CloseConnection();
                }
            }

            if (filePath != null)
            {
                var result = MessageBox.Show("Хотите чтобы ИИ обработал данные файла?", "Повторить?",
                    MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes) await InitializeAsync();
            }
        }
    }
    public void CreateLocalDatabase()
    {
        var conString = "Server=localhost;Trusted_Connection=True;";
        using (SqlConnection connection = new SqlConnection(conString))
        {
            connection.Open();
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "BEGIN TRY CREATE DATABASE local_database; END TRY BEGIN CATCH IF ERROR_NUMBER() <> 1801 BEGIN THROW; END; END CATCH;";
            cmd.Connection = connection;
            cmd.ExecuteNonQuery();
            connection.Close();
        }
    }


}