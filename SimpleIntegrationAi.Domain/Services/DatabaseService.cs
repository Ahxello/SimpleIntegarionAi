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
            if(field.ToLower().Substring(1).Contains("id"))
                createTableQuery += $"{field} INT PRIMARY KEY,";
            else if (field.ToLower().Contains("id"))
                createTableQuery += $"{field} INT,";
            else
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
    public void DeleteField(string tableName, string fieldName)
    {
        using (var command = new SqlCommand($"ALTER TABLE {tableName} DROP COLUMN {fieldName};", _connection))
        {
            command.ExecuteNonQuery();
        }
    }

    public DataTable GetData(string tableName)
    {
        var data = new DataTable();
        using (var adapter = new SqlDataAdapter($"SELECT * FROM {tableName};", _connection))
        {
            adapter.Fill(data);
        }
        return data;
    }
    public void AddData(string tableName, DataTable data)
    {
        if (data == null || data.Rows.Count == 0)
            return;

        foreach (DataRow row in data.Rows)
        {
            var columns = string.Join(",", data.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            var values = string.Join(",", row.ItemArray.Select(v => $"'{v}'"));
            using (var command = new SqlCommand($"INSERT INTO {tableName} ({columns}) VALUES ({values});", _connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }
    public void DeleteData(string tableName, DataTable data)
    {
        if (data == null || data.Rows.Count == 0)
            return;

        foreach (DataRow row in data.Rows)
        {
            var conditions = string.Join(" AND ", data.Columns.Cast<DataColumn>().Select(c => $"{c.ColumnName}='{row[c.ColumnName]}'"));
            using (var command = new SqlCommand($"DELETE FROM {tableName} WHERE {conditions};", _connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }

    //Need Fix else
    public void UpdateData(string tableName, DataTable data)
    {
        if (data == null || data.Rows.Count == 0)
            return;

        foreach (DataRow row in data.Rows)
        {
            var columns = string.Join(",", data.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            var values = string.Join(",", row.ItemArray.Select(v => $"'{v}'"));
            using (var command = new SqlCommand($"UPDATE {tableName} ({columns}) VALUES ({values});", _connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }
    public void DeleteRow(string tableName, string primaryKey, object keyValue)
    {
        var deleteQuery = $"DELETE FROM [{tableName}] WHERE [{primaryKey}] = @KeyValue";

        using (var command = new SqlCommand(deleteQuery, _connection))
        {
            command.Parameters.AddWithValue("@KeyValue", keyValue);
            command.ExecuteNonQuery();
        }
    }

    
    public void AddForeignKey(Relationship relationship)
    {
        string constraintName = $"FK_{relationship.From}_{relationship.To}";
        string addForeignKeyQuery;

        switch (relationship.Type)
        {
            case RelationshipType.OneToOne:
                addForeignKeyQuery = $"ALTER TABLE [{relationship.To}] ADD CONSTRAINT [{constraintName}] FOREIGN KEY ([{relationship.ForeignKey}]) REFERENCES [{relationship.From}] ([{relationship.ParentKey}]);";
                break;
            case RelationshipType.OneToMany:
                addForeignKeyQuery = $"ALTER TABLE [{relationship.To}] ADD CONSTRAINT [{constraintName}] FOREIGN KEY ([{relationship.ForeignKey}]) REFERENCES [{relationship.From}] ([{relationship.ParentKey}]);";
                break;
            case RelationshipType.ManyToOne:
                addForeignKeyQuery = $"ALTER TABLE [{relationship.From}] ADD CONSTRAINT [{constraintName}] FOREIGN KEY ([{relationship.ParentKey}]) REFERENCES [{relationship.To}] ([{relationship.ForeignKey}]);";
                break;
            case RelationshipType.ManyToMany:
                throw new NotImplementedException("Many-to-Many relationships require a junction table and additional logic.");
            default:
                throw new ArgumentOutOfRangeException();
        }

        using (var command = new SqlCommand(addForeignKeyQuery, _connection))
        {
            command.ExecuteNonQuery();
        }
    }
    public void Dispose()
    {
        CloseConnection();
    }
}