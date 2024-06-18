using SimpleIntegrationAi.Domain.Models;
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

    public void Dispose()
    {
        CloseConnection();
    }
}