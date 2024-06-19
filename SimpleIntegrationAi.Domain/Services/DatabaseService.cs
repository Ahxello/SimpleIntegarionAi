using SimpleIntegrationAi.Domain.Models;
using System.Data;
using System.Data.SqlClient;

namespace SimpleIntegrationAi.Domain.Services;

public class DatabaseService : IDisposable
{
    private readonly string _connectionString;
    private SqlConnection _connection;

    public DatabaseService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void OpenConnection()
    {
        _connection = new SqlConnection(_connectionString);
        _connection.Open();
    }

    public void CloseConnection()
    {
        if (_connection != null)
        {
            _connection.Close();
            _connection.Dispose();
        }
    }

    public void CreateTable(Entity entity)
    {
        string tableName = entity.Name;
        string createTableQuery = $"IF OBJECT_ID(N'{tableName}') IS NULL CREATE TABLE {tableName} (";

        foreach (var field in entity.Fields)
        {
            createTableQuery += $"{field} NVARCHAR(MAX), ";
        }

        // Удаляем последнюю запятую и пробел
        createTableQuery = createTableQuery.TrimEnd(',', ' ');
        createTableQuery += ");";

        using (var command = new SqlCommand(createTableQuery, _connection))
        {
            command.ExecuteNonQuery();
        }
    }

    public void InsertData(Entity entity)
    {
        string tableName = entity.Name;

        foreach (var row in entity.Data)
        {
            string insertQuery = $"INSERT INTO {tableName} (";
            foreach (var field in entity.Fields)
            {
                insertQuery += $"{field}, ";
            }

            // Удаляем последнюю запятую и пробел
            insertQuery = insertQuery.TrimEnd(',', ' ');
            insertQuery += ") VALUES (";

            foreach (var field in entity.Fields)
            {
                insertQuery += $"@{field}, ";
            }

            // Удаляем последнюю запятую и пробел
            insertQuery = insertQuery.TrimEnd(',', ' ');
            insertQuery += ");";

            using (var command = new SqlCommand(insertQuery, _connection))
            {
                foreach (var field in entity.Fields)
                {
                    command.Parameters.AddWithValue($"@{field}", row[field]);
                }

                command.ExecuteNonQuery();
            }
        }
    }
    public void AddForeignKey(Relationship relationship)
    {
        string addForeignKeyQuery = $@"
            ALTER TABLE {relationship.From} 
            ADD CONSTRAINT FK_{relationship.From}_{relationship.To} 
            FOREIGN KEY ({relationship.Type}) REFERENCES {relationship.To}(Id);";

        using (var command = new SqlCommand(addForeignKeyQuery, _connection))
        {
            command.ExecuteNonQuery();
        }
    }
    public List<string> GetTableNames()
    {
        var tableNames = new List<string>();
        string query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";

        using (var command = new SqlCommand(query, _connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                tableNames.Add(reader.GetString(0));
            }
        }

        return tableNames;
    }

    public void RenameTable(string oldName, string newName)
    {
        string query = $"EXEC sp_rename '{oldName}', '{newName}'";

        using (var command = new SqlCommand(query, _connection))
        {
            command.ExecuteNonQuery();
        }
    }

    public void RenameField(string tableName, string oldFieldName, string newFieldName)
    {
        string query = $"EXEC sp_rename '{tableName}.{oldFieldName}', '{newFieldName}', 'COLUMN'";

        using (var command = new SqlCommand(query, _connection))
        {
            command.ExecuteNonQuery();
        }
    }

    public void AddField(string tableName, string fieldName, string fieldType)
    {
        string query = $"ALTER TABLE {tableName} ADD {fieldName} {fieldType}";

        using (var command = new SqlCommand(query, _connection))
        {
            command.ExecuteNonQuery();
        }
    }

    public DataTable GetData(string tableName)
    {
        var dataTable = new DataTable();

        using (var command = new SqlCommand($"SELECT * FROM {tableName};", _connection))
        {
            using (var adapter = new SqlDataAdapter(command))
            {
                adapter.Fill(dataTable);
            }
        }

        return dataTable;
    }
    public void Dispose()
    {
        CloseConnection();
    }
}