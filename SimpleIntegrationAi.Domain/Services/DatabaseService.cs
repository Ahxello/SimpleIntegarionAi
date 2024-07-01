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
            string res = field.ToLower().Substring(0,2);
            if (field.ToLower().Substring(0,2).Contains("id"))
                createTableQuery += $"{field} INT PRIMARY KEY,";
            else if (field.ToLower().Contains("id"))
                createTableQuery += $"{field} INT,";
            else
                createTableQuery += $"{field} NVARCHAR(MAX) NOT NULL, ";
        }

        // Удаляем последнюю запятую и пробел
        createTableQuery = createTableQuery.TrimEnd(',', ' ');
        createTableQuery += ");";

        using (SqlCommand command = new SqlCommand(createTableQuery, _connection))
        {
            command.ExecuteNonQuery();
        }
    }

    public void InsertData(Entity entity)
    {
        string tableName = entity.Name;

        foreach (var row in entity.Data)
        {
            if (row.Count > 0)
            {
                string insertQuery = $"INSERT INTO {tableName} (";
                foreach (string field in entity.Fields)
                {
                    insertQuery += $"{field}, ";
                }

                // Удаляем последнюю запятую и пробел
                insertQuery = insertQuery.TrimEnd(',', ' ');
                insertQuery += ") VALUES (";

                foreach (string field in entity.Fields)
                {
                    insertQuery += $"@{field}, ";
                }

                // Удаляем последнюю запятую и пробел
                insertQuery = insertQuery.TrimEnd(',', ' ');
                insertQuery += ");";

                using (SqlCommand command = new SqlCommand(insertQuery, _connection))
                {
                    foreach (string field in entity.Fields)
                    {
                        command.Parameters.AddWithValue($"@{field}", row[field]);
                    }

                    command.ExecuteNonQuery();
                }
            }
            else
                continue;
            
        }
    }
   
    public List<string> GetTableNames()
    {
        List<string> tableNames = new List<string>();
        string query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";

        using (SqlCommand command = new SqlCommand(query, _connection))
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

        using (SqlCommand command = new SqlCommand(query, _connection))
        {
            command.ExecuteNonQuery();
        }
    }

    public void RenameField(string tableName, string oldFieldName, string newFieldName)
    {
        string query = $"EXEC sp_rename '{tableName}.{oldFieldName}', '{newFieldName}', 'COLUMN'";

        using (SqlCommand command = new SqlCommand(query, _connection))
        {
            command.ExecuteNonQuery();
        }
    }

    public void AddField(string tableName, string fieldName, string fieldType)
    {
        string query = $"ALTER TABLE {tableName} ADD {fieldName} {fieldType}";

        using (SqlCommand command = new SqlCommand(query, _connection))
        {
            command.ExecuteNonQuery();
        }
    }
    public void DeleteField(string tableName, string fieldName)
    {
        using (SqlCommand command = new SqlCommand($"ALTER TABLE {tableName} DROP COLUMN {fieldName};", _connection))
        {
            command.ExecuteNonQuery();
        }
    }

    public DataTable GetData(string tableName)
    {
        DataTable data = new DataTable();
        using (SqlDataAdapter adapter = new SqlDataAdapter($"SELECT * FROM {tableName};", _connection))
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
            string columns = string.Join(",", data.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            string values = string.Join(",", row.ItemArray.Select(v => $"'{v}'"));
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
            string conditions = string.Join(" AND ", data.Columns.Cast<DataColumn>().Select(c => $"{c.ColumnName}='{row[c.ColumnName]}'"));
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
            string columns = string.Join(",", data.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            string values = string.Join(",", row.ItemArray.Select(v => $"'{v}'"));
            using (SqlCommand command = new SqlCommand($"UPDATE {tableName} ({columns}) VALUES ({values});", _connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }
    public void DeleteRow(string tableName, string primaryKey, object keyValue)
    {
        string deleteQuery = $"DELETE FROM [{tableName}] WHERE [{primaryKey}] = @KeyValue";

        using (SqlCommand command = new SqlCommand(deleteQuery, _connection))
        {
            command.Parameters.AddWithValue("@KeyValue", keyValue);
            command.ExecuteNonQuery();
        }
    }

    
    public void AddForeignKey(List<Relationship> relationships)
    {
        foreach(var relationship in relationships)
        {
            string constraintName = $"FK_{relationship.FromTable}{relationship.FromField}_{relationship.ToTable}{relationship.ToField}";
            string addForeignKeyQuery;

            switch (relationship.Type)
            {
                case RelationshipType.OneToOne:
                    addForeignKeyQuery = $"ALTER TABLE [{relationship.ToTable}] ADD CONSTRAINT [{constraintName}] FOREIGN KEY ([{relationship.ToField}]) REFERENCES [{relationship.FromTable}] ([{relationship.FromField}]) ON DELETE CASCADE ON UPDATE CASCADE;";
                    break;
                case RelationshipType.OneToMany:
                    addForeignKeyQuery = $"ALTER TABLE [{relationship.ToTable}] ADD CONSTRAINT [{constraintName}] FOREIGN KEY ([{relationship.ToField}]) REFERENCES [{relationship.FromTable}] ([{relationship.FromField}]) ON DELETE CASCADE ON UPDATE CASCADE;";
                    break;
                case RelationshipType.ManyToOne:
                    addForeignKeyQuery = $"ALTER TABLE [{relationship.FromTable}] ADD CONSTRAINT [{constraintName}] FOREIGN KEY ([{relationship.FromField}]) REFERENCES [{relationship.ToTable}] ([{relationship.ToField}]) ON DELETE CASCADE ON UPDATE CASCADE;";
                    break;
                case RelationshipType.ManyToMany:
                    throw new NotImplementedException("Many-to-Many relationships require a junction table and additional logic.");
                default:
                    throw new ArgumentOutOfRangeException();
            }

            using (SqlCommand command = new SqlCommand(addForeignKeyQuery, _connection))
            {
                command.ExecuteNonQuery();
            }
        }
        
    }
    public void Dispose()
    {
        CloseConnection();
    }
}