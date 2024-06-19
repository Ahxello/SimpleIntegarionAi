using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Input;
using AiTestLibrary.Interfaces;
using GemBox.Document;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using SimpleIntegrationAi.Domain.Models;
using SimpleIntegrationAi.Domain.Services;
using SimpleIntegrationAi.WPF.Commands;
using File = System.IO.File;

namespace SimpleIntegrationAi.WPF.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IGeminiGpt _geminiGpt;

    private readonly AsyncCommand _loadApiResponseCommand;

    private readonly Command _loadTablesCommand;

    private readonly Command _renameTableCommand;

    private readonly Command _renameFieldCommand;

    private readonly Command _addFieldCommand;

    private readonly Command _loadDataCommand;

    private readonly AsyncCommand _loadFileCommand;

    private readonly IResponseParser _responseParser;

    private readonly IYandexGpt _yandexGpt;

    private readonly string connectionString = "Server=localhost;Database=local_database;Trusted_Connection=True;";

    private readonly string foledrId = "b1gphb1c693npe94nmrv";

    private readonly string iamtoken =
        "t1.9euelZqYnZ3ImpyelJ6ayZKal5aRz-3rnpWam46WzZKSlMaVjsycjZCZksjl8_dna0pM-e9kdS9d_N3z9ycaSEz572R1L138zef1656Vmp6WxpXNks3Il5GJnsiRkpiY7_zF656Vmp6WxpXNks3Il5GJnsiRkpiY.Xuyhnc6xzrdkOPKi54WTlnMny4T41YI7FVYrOueiPFfjxVuv7fBYLrSJFanKJYAHvq7PrEXYAsnSe3J2iw7vDQ";

    private DataTable _data;

    private ObservableCollection<Entity> _databaseEntities;
    private ObservableCollection<string> _fields;
    private string _newFieldName;
    private string _newFieldType;
    private string _newTableName;
    private string _selectedField;
    private string _selectedTable;
    private Product _selectedTemporaryEntity;
    private ObservableCollection<string> _tables;
    private ObservableCollection<Product> _temporaryEntities;

    public MainWindowViewModel(IYandexGpt yandexGpt, IResponseParser responseParser, IGeminiGpt geminiGpt)
    {
        _yandexGpt = yandexGpt;

        _temporaryEntities = new ObservableCollection<Product>();

        _databaseEntities = new ObservableCollection<Entity>();

        _responseParser = responseParser;

        _geminiGpt = geminiGpt;

        _loadApiResponseCommand = new AsyncCommand(LoadApiResponse);

        _loadTablesCommand = new Command(LoadTables);

        _renameTableCommand = new Command(RenameTable);

        _addFieldCommand = new Command(AddField);

        _renameFieldCommand = new Command(RenameField);

        _loadDataCommand = new Command(LoadData);

        FieldTypes = new ObservableCollection<string> { "NVARCHAR(MAX)", "INT", "FLOAT", "DATETIME" }; // Пример типов полей
        CreateLocalDatabase();
        LoadTables();
    }

    public ICommand LoadTablesCommand => _loadTablesCommand;

    public ICommand RenameTableCommand => _renameTableCommand;

    public ICommand AddFieldCommand => _addFieldCommand;

    public ICommand RenameFieldCommand => _renameFieldCommand;

    public ICommand LoadDataCommand => _loadDataCommand;
    public string SelectedTable
    {
        get => _selectedTable;
        set
        {
            SetField(ref _selectedTable, value);
            LoadFields();
            LoadData();
        } 
    }
    public ObservableCollection<string> FieldTypes { get; set; }
    public string NewTableName
    {
        get => _newTableName;
        set => SetField(ref _newTableName, value);
    }

    public string SelectedField
    {
        get => _selectedField;
        set => SetField(ref _selectedField, value);
    }

    public string NewFieldName
    {
        get => _newFieldName;
        set => SetField(ref _newFieldName, value);
    }

    public string NewFieldType
    {
        get => _newFieldType;
        set => SetField(ref _newFieldType, value);
    }

    public ObservableCollection<string> Tables
    {
        get => _tables;
        set => SetField(ref _tables, value);
    }

    public ObservableCollection<string> Fields
    {
        get => _fields;
        set => SetField(ref _fields, value);
    }

    public DataTable Data
    {
        get => _data;
        set => SetField(ref _data, value);
    }

    public ICommand LoadFileCommand => _loadFileCommand;
    public ICommand LoadApiResponseCommand => _loadApiResponseCommand;


    public ObservableCollection<Product> TemporaryEntities
    {
        get => _temporaryEntities;
        set => SetField(ref _temporaryEntities, value);
    }


    private void LoadTables()
    {
        using (var dbHelper = new DatabaseService(connectionString))
        {
            dbHelper.OpenConnection();
            var tables = dbHelper.GetTableNames();
            Tables = new ObservableCollection<string>(tables);
            dbHelper.CloseConnection();
        }
        LoadFields();
        LoadData();
    }

    private void RenameTable()
    {
        if (string.IsNullOrEmpty(SelectedTable) || string.IsNullOrEmpty(NewTableName))
            return;

        using (var dbHelper = new DatabaseService(connectionString))
        {
            dbHelper.OpenConnection();
            dbHelper.RenameTable(SelectedTable, NewTableName);
            dbHelper.CloseConnection();
        }

        LoadTables();
    }

    private void RenameField()
    {
        if (string.IsNullOrEmpty(SelectedTable) || string.IsNullOrEmpty(SelectedField) || string.IsNullOrEmpty(NewFieldName))
            return;

        using (var dbHelper = new DatabaseService(connectionString))
        {
            dbHelper.OpenConnection();
            dbHelper.RenameField(SelectedTable, SelectedField, NewFieldName);
            dbHelper.CloseConnection();
        }

        LoadFields();
        LoadData();
    }

    private void AddField()
    {
        if (string.IsNullOrEmpty(SelectedTable) || string.IsNullOrEmpty(NewFieldName) || string.IsNullOrEmpty(NewFieldType))
            return;

        using (var dbHelper = new DatabaseService(connectionString))
        {
            dbHelper.OpenConnection();
            dbHelper.AddField(SelectedTable, NewFieldName, NewFieldType);
            dbHelper.CloseConnection();
        }

        LoadFields();
        LoadData();
    }

    private void LoadFields()
    {
        if (string.IsNullOrEmpty(SelectedTable))
            return;

        using (var dbHelper = new DatabaseService(connectionString))
        {
            dbHelper.OpenConnection();
            var data = dbHelper.GetData(SelectedTable);
            if (data.Rows.Count > 0)
            {
                Fields = new ObservableCollection<string>(data.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            }
            else
            {
                Fields = new ObservableCollection<string>();
            }
            dbHelper.CloseConnection();
        }
    }

    private void LoadData()
    {
        if (string.IsNullOrEmpty(SelectedTable))
            return;

        using (var dbHelper = new DatabaseService(connectionString))
        {
            dbHelper.OpenConnection();
            var data = dbHelper.GetData(SelectedTable);
            Data = data;
            dbHelper.CloseConnection();
        }
    }


    public async Task InitializeAsync()
    {
        var result = string.Join(" ", TemporaryEntities.Select(i => i.Message));

        var messageCollection =
            await _geminiGpt.Request(
                $"{result} Привет, сейчас я отправлю тебе список. Тебе нужно будет его разделить на справочные сущности и обычные сущности, также указать связи между сущностями для реляционной базы данных. Справочная сущность будет иметь только поля Id и Name.\r\nВот примерная структура твоего ответа\r\nEntities: \r\nEntity 1\r\n\tFields\r\nEntity 2\r\n\tFields\r\nEntity 3\r\n\tFields\r\n\r\nRelationships: \r\n Entity 1 - Entity 2: Relationship",
                "AIzaSyDQFWBEW9yx2wduHg_fE38oOeEgrFOPtI8");
        var json = JObject.Parse(messageCollection);
        var text = json["result"]["alternatives"][0]["message"]["text"].ToString();


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

                //var document = DocumentModel.Load(filePath);

                var lines = File.ReadAllLines(filePath);
                var parser = new ResponseParser();
                var ents = parser.Parse(lines);
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

            else if (fileExtension == ".txt")
            {
                var lines = File.ReadAllLines(filePath);
                var parser = new ResponseParser();
                var ents = parser.Parse(lines);
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
        }
    }

    public void CreateLocalDatabase()
    {
        var conString = "Server=localhost;Trusted_Connection=True;";
        using (var connection = new SqlConnection(conString))
        {
            connection.Open();
            var cmd = new SqlCommand();
            cmd.CommandText =
                "BEGIN TRY CREATE DATABASE local_database; END TRY BEGIN CATCH IF ERROR_NUMBER() <> 1801 BEGIN THROW; END; END CATCH;";
            cmd.Connection = connection;
            cmd.ExecuteNonQuery();
            connection.Close();
        }
    }
}