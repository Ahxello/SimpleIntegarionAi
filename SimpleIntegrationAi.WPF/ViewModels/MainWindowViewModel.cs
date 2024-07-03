using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
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
    private ObservableCollection<Entity> _entities;
    private ObservableCollection<Relationship> _relationships;
    private readonly AsyncCommand _loadApiResponseCommand;

    private readonly Command _loadTablesCommand;

    private readonly Command _renameTableCommand;

    private readonly Command _renameFieldCommand;

    private readonly Command _addFieldCommand;

    private readonly Command _loadDataCommand;

    private readonly AsyncCommand _loadFileCommand;

    private readonly AsyncCommand _addFiveFieldsCommand;

    private readonly Command _deleteFieldCommand;

    private readonly Command _deleteDataCommand;

    private readonly Command _addDataCommand;

    private readonly Command _saveDataCommand;

    private readonly Command _addForeignKeyCommand;

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
    
    private DataRowView _selectedDataRow;

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

        _addFiveFieldsCommand = new AsyncCommand(AddFiveFields);

        FieldTypes = new ObservableCollection<string> { "NVARCHAR(MAX)", "INT", "FLOAT", "DATETIME" }; // Пример типов полей
       
        _relationshipTypes = new ObservableCollection<RelationshipType>
    {
        RelationshipType.OneToOne,
        RelationshipType.OneToMany,
        RelationshipType.ManyToOne,
        RelationshipType.ManyToMany
    };
        
        _entities = new ObservableCollection<Entity>();
       
        _relationships = new ObservableCollection<Relationship>();
        
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
    
    public ObservableCollection<Entity> Entities
    {
        get => _entities;
        set
        {
            SetField(ref _entities, value);
        }
    }
    
    public ObservableCollection<Relationship> Relationships
    {
        get => _relationships;
        set
        {
            SetField(ref _relationships, value);
        }
    }
    
    public ICommand LoadTablesCommand => _loadTablesCommand;

    public ICommand AddFiveFieldsCommand => _addFiveFieldsCommand;

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
        Tables = new ObservableCollection<string>(_entities.Select(e => e.Name));
        LoadFields();
        LoadData();
    }

    private void RenameTable()
    {
        if (string.IsNullOrEmpty(SelectedTable) || string.IsNullOrEmpty(NewTableName))
            return;

        var entity = _entities.FirstOrDefault(e => e.Name == SelectedTable);
        if (entity != null)
        {
            entity.Name = NewTableName;
        }

        LoadTables();
    }

    private void RenameField()
    {
        if (string.IsNullOrEmpty(SelectedTable) || string.IsNullOrEmpty(SelectedField) || string.IsNullOrEmpty(NewFieldName))
            return;

        var entity = _entities.FirstOrDefault(e => e.Name == SelectedTable);
        if (entity != null)
        {
            var fieldIndex = entity.Fields.IndexOf(SelectedField);
            if (fieldIndex != -1)
            {
                entity.Fields[fieldIndex] = NewFieldName;
            }
        }

        LoadFields();
        LoadData();
    }

    private void AddField()
    {
        if (string.IsNullOrEmpty(SelectedTable) || string.IsNullOrEmpty(NewFieldName) || string.IsNullOrEmpty(NewFieldType))
            return;

        var entity = _entities.FirstOrDefault(e => e.Name == SelectedTable);
        if (entity != null)
        {
            entity.Fields.Add(NewFieldName);
        }

        LoadFields();
        LoadData();
    }
    private async Task AddFiveFields()
    {
        if (string.IsNullOrEmpty(SelectedTable))
            return;

        Entity entity = _entities.FirstOrDefault(e => e.Name == SelectedTable);
        if (entity != null)
        {
            string newFields = await _chatGpt.AddFields(entity.Name, entity.Fields);
            string[] lines = Regex.Split(newFields, "\r\n|\r|\n");
            foreach (string line in lines) 
            {
                entity.Fields.Add(line);
            }

        }
        LoadFields();
        LoadData();
    }
    private void LoadFields()
    {
        if (string.IsNullOrEmpty(SelectedTable))
            return;

        var entity = _entities.FirstOrDefault(e => e.Name == SelectedTable);
        if (entity != null)
        {
            Fields = new ObservableCollection<string>(entity.Fields);
        }
        else
        {
            Fields = new ObservableCollection<string>();
        }
    }

    private void DeleteField()
    {
        if (string.IsNullOrEmpty(SelectedTable) || string.IsNullOrEmpty(SelectedField))
            return;

        var entity = _entities.FirstOrDefault(e => e.Name == SelectedTable);
        if (entity != null)
        {
            entity.Fields.Remove(SelectedField);
        }

        LoadFields();
        LoadData();
    }

    private void LoadData()
    {
        if (string.IsNullOrEmpty(SelectedTable))
            return;

        var entity = _entities.FirstOrDefault(e => e.Name == SelectedTable);
        if (entity != null)
        {
            var dataTable = new DataTable();
            foreach (var field in entity.Fields)
            {
                dataTable.Columns.Add(field);
            }

            foreach (var data in entity.Data)
            {
                var row = dataTable.NewRow();
                foreach (var field in entity.Fields)
                {
                    if (data.ContainsKey(field))
                    {
                        row[field] = data[field];
                    }
                    else
                    {
                        row[field] = DBNull.Value;
                    }
                }
                dataTable.Rows.Add(row);
            }

            Data = dataTable;
        }
    }

    private void DeleteData()
    {
        if (SelectedDataRow == null) return;

        var entity = _entities.FirstOrDefault(e => e.Name == SelectedTable);
        if (entity != null)
        {
            var row = entity.Data.FirstOrDefault(d => d["ID"] == SelectedDataRow.Row["ID"].ToString());
            if (row != null)
            {
                entity.Data.Remove(row);
            }
        }

        LoadData();
    }

    private void SaveData()
    {
        if (Data == null || Data.Rows.Count == 0)
            return;

        var entity = _entities.FirstOrDefault(e => e.Name == SelectedTable);
        if (entity != null)
        {
            entity.Data.Clear();
            foreach (DataRow row in Data.Rows)
            {
                var data = new Dictionary<string, string>();
                foreach (DataColumn column in Data.Columns)
                {
                    data[column.ColumnName] = row[column].ToString();
                }
                entity.Data.Add(data);
            }
        }

        LoadData();
    }

    private void AddData()
    {
        if (string.IsNullOrEmpty(SelectedTable))
            return;

        var entity = _entities.FirstOrDefault(e => e.Name == SelectedTable);
        if (entity != null)
        {
            var data = new Dictionary<string, string>();
            foreach (var field in entity.Fields)
            {
                data[field] = GetDefaultValueForType(field).ToString();
            }
            entity.Data.Add(data);
        }

        LoadData();
    }

    private object GetDefaultValueForType(string type)
    {
        return type switch
        {
            "NVARCHAR(MAX)" => "",
            "INT" => 0,
            "FLOAT" => 0.0,
            "DATETIME" => DateTime.Now.ToString(),
            _ => null
        };
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

                ResponseParser parser = new ResponseParser();
                (List<Entity> entities, List<Relationship> relationships) = parser.Parse(filePath);
                _entities = new ObservableCollection<Entity>(entities);
                _relationships = new ObservableCollection<Relationship>(relationships);
            }
            else if (fileExtension == ".txt")
            {
                string fileText = File.ReadAllText(filePath);
                string responseText = await _chatGpt.JsonRequest($"{fileText}");

                ResponseParser parser = new ResponseParser();
                (List<Entity> entities, List<Relationship> relationships) = parser.Parse(responseText);
                _entities = new ObservableCollection<Entity>(entities);
                _relationships = new ObservableCollection<Relationship>(relationships);
            }
            LoadTables();
        }
    }

}