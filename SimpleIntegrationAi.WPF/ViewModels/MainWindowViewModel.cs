﻿using System.Collections.ObjectModel;
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

    private readonly AsyncCommand _loadApiResponseCommand;

    private readonly Command _loadTablesCommand;

    private readonly Command _renameTableCommand;

    private readonly Command _renameFieldCommand;

    private readonly Command _addFieldCommand;

    private readonly Command _loadDataCommand;

    private readonly AsyncCommand _loadFileCommand;

    private readonly IResponseParser _responseParser;

    private readonly IChatGpt _chatGpt;

    private readonly string connectionString = "Server=localhost;Database=local_database;Trusted_Connection=True;";
    private DataTable _data;
    private ObservableCollection<string> _fields;
    private string _newFieldName;
    private string _newFieldType;
    private string _newTableName;
    private string _selectedField;
    private string _selectedTable;
    private ObservableCollection<string> _tables;
    private readonly Command _deleteFieldCommand;
    private readonly Command _deleteDataCommand;
    private readonly Command _addDataCommand;
    private readonly Command _saveDataCommand;
    private DataRowView _selectedDataRow;
    private readonly Command _addForeignKeyCommand;
    private ObservableCollection<RelationshipType> _relationshipTypes;
    public MainWindowViewModel(IChatGpt chatGpt, IResponseParser responseParser, IGeminiGpt geminiGpt)
    {
        _chatGpt = chatGpt;


        _responseParser = responseParser;


        _loadApiResponseCommand = new AsyncCommand(LoadApiResponse);

        _loadTablesCommand = new Command(LoadTables);

        _renameTableCommand = new Command(RenameTable);

        _addFieldCommand = new Command(AddField);

        _renameFieldCommand = new Command(RenameField);

        _loadDataCommand = new Command(LoadData);

        _deleteFieldCommand = new Command(DeleteField);

        _deleteDataCommand = new Command(DeleteData);

        _addDataCommand = new Command(AddData);

        _saveDataCommand = new Command(SaveData);

        FieldTypes = new ObservableCollection<string> { "NVARCHAR(MAX)", "INT", "FLOAT", "DATETIME" }; // Пример типов полей
        _relationshipTypes = new ObservableCollection<RelationshipType>
    {
        RelationshipType.OneToOne,
        RelationshipType.OneToMany,
        RelationshipType.ManyToOne,
        RelationshipType.ManyToMany
    };

        CreateLocalDatabase();
        LoadTables();
    }

    public ObservableCollection<RelationshipType> RelationshipTypes
    {
        get => _relationshipTypes;
        set
        {
            SetField(ref _relationshipTypes, value);
            
        }
    }

    public ICommand LoadTablesCommand => _loadTablesCommand;

    public ICommand RenameTableCommand => _renameTableCommand;

    public ICommand AddFieldCommand => _addFieldCommand;

    public ICommand RenameFieldCommand => _renameFieldCommand;

    public ICommand DeleteFieldCommand => _deleteFieldCommand;

    public ICommand DeleteDataCommand => _deleteDataCommand;

    public ICommand AddDataCommand => _addDataCommand;

    public ICommand LoadDataCommand => _loadDataCommand;

    public ICommand SaveDataCommand => _saveDataCommand;
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

    public DataRowView SelectedDataRow
    {
        get => _selectedDataRow;
        set
        {
            SetField(ref _selectedDataRow, value);
            
        }
    }

    private string _selectedParentEntity;
    public string SelectedParentEntity
    {
        get => _selectedParentEntity;
        set
        {
            SetField(ref _selectedParentEntity, value);
            
        }
    }

    private string _selectedChildEntity;
    public string SelectedChildEntity
    {
        get => _selectedChildEntity;
        set
        {
            SetField(ref _selectedChildEntity, value);
            
        }
    }

    private RelationshipType _selectedRelationshipType;
    public RelationshipType SelectedRelationshipType
    {
        get => _selectedRelationshipType;
        set
        {
            SetField(ref _selectedRelationshipType, value);
            
        }
    }


    private string _selectedForeignKey;
    public string SelectedForeignKey
    {
        get => _selectedForeignKey;
        set
        {
          SetField(ref _selectedForeignKey, value);
          
        }
    }

    private string _selectedParentKey;
    public string SelectedParentKey
    {
        get => _selectedParentKey;
        set
        {
            SetField(ref _selectedParentKey, value);
            
        }
    }
    public ICommand LoadFileCommand => _loadFileCommand;
    public ICommand LoadApiResponseCommand => _loadApiResponseCommand;



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

        using (DatabaseService dbHelper = new DatabaseService(connectionString))
        {
            dbHelper.OpenConnection();
            DataTable data = dbHelper.GetData(SelectedTable);
            if (data.Rows.Count > 0)
                Fields = new ObservableCollection<string>
                    (data.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            else
                Fields = new ObservableCollection<string>();
            dbHelper.CloseConnection();
        }
    }

    private void DeleteField()
    {
        if (string.IsNullOrEmpty(SelectedTable) || string.IsNullOrEmpty(SelectedField))
            return;

        using (DatabaseService dbHelper = new DatabaseService(connectionString))
        {
            dbHelper.OpenConnection();
            dbHelper.DeleteField(SelectedTable, SelectedField);
            dbHelper.CloseConnection();
        }

        LoadFields();
        LoadData();
    }
    private void LoadData()
    {
        if (string.IsNullOrEmpty(SelectedTable))
            return;

        using (DatabaseService dbHelper = new DatabaseService(connectionString))
        {
            dbHelper.OpenConnection();
            DataTable data = dbHelper.GetData(SelectedTable);
            Data = data;
            dbHelper.CloseConnection();
        }
    }
    private void DeleteData()
    {
        if (SelectedDataRow == null) return;

        // Removing the row from DataTable
        SelectedDataRow.Row.Delete();

        using (var helper = new DatabaseService(connectionString))
        {
            helper.OpenConnection();

            string primaryKey = Data.Columns[0].ColumnName;
            var keyValue = SelectedDataRow.Row[primaryKey];

            // Deleting the row from database
            helper.DeleteRow(SelectedTable, primaryKey, keyValue);
        }
        LoadData();
    }
    private void SaveData()
    {
        if (Data == null || Data.Rows.Count == 0)
            return;
        using (var dbHelper = new DatabaseService(connectionString))
        {
            dbHelper.OpenConnection();
            dbHelper.UpdateData(SelectedTable, Data);
        }

        LoadData();
    }
    private void AddData()
    {
        if (string.IsNullOrEmpty(SelectedTable))
            return;

        using (var dbHelper = new DatabaseService(connectionString))
        {
            dbHelper.OpenConnection();
            dbHelper.AddData(SelectedTable, Data);
            dbHelper.CloseConnection();
        }

        LoadData();
    }


    private async Task LoadApiResponse()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Text Files (*.txt)|*.txt|Word Documents (*.docx)|*.docx|All Files (*.*)|*.*"
        };
        if (openFileDialog.ShowDialog() == true)
        {
            string filePath = openFileDialog.FileName;
            string fileExtension = Path.GetExtension(filePath);

            if (fileExtension == ".docx")
            {
                ComponentInfo.SetLicense("FREE-LIMITED-KEY");

                //var document = DocumentModel.Load(filePath);
                ResponseParser parser = new ResponseParser();
                (List<Entity> entities, List<Relationship> relationships) = parser.Parse(filePath);
                using (DatabaseService dbService = new DatabaseService(connectionString))
                {
                    dbService.OpenConnection();

                    foreach (var entity in entities)
                    {
                        dbService.CreateTable(entity);
                        dbService.InsertData(entity);
                    }
                    dbService.AddForeignKey(relationships);
                    dbService.CloseConnection();
                }
            }

            else if (fileExtension == ".txt")
            {

                string fileText = File.ReadAllText(filePath);
                string responseText =
                        await _chatGpt.JsonRequest($"{fileText}");
                ResponseParser parser = new ResponseParser();
                (List<Entity> entities, List<Relationship> relationships) = parser.Parse(responseText);
                using (DatabaseService dbService = new DatabaseService(connectionString))
                {
                    dbService.OpenConnection();

                    foreach (var entity in entities)
                    {
                        dbService.CreateTable(entity);
                        dbService.InsertData(entity);
                    }
                    dbService.AddForeignKey(relationships);
                    dbService.CloseConnection();
                }
                LoadTables();

            }
        }
    }
    public void CreateLocalDatabase()
    {
        string conString = "Server=localhost;Trusted_Connection=True;";
        using (SqlConnection connection = new SqlConnection(conString))
        {
            connection.Open();
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText =
                "BEGIN TRY CREATE DATABASE local_database; END TRY BEGIN CATCH IF ERROR_NUMBER() <> 1801 BEGIN THROW; END; END CATCH;";
            cmd.Connection = connection;
            cmd.ExecuteNonQuery();
            connection.Close();
        }
    }
    
}