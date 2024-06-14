using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleIntegrationAi.Domain.Models;
using System.Text;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace SimpleIntegrationAi.Domain.Services;

public class ResponseParser : IResponseParser
{
    private string connectionString = "sadadsa";

    public string[] GetMessageAsync(string json)
    {
        try
        {
            JObject parsedJson = JObject.Parse(json);
            string text = parsedJson["result"]["alternatives"][0]["message"]["text"].ToString();

            return text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            ;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Ошибка при парсинге JSON: {ex.Message}");
            return new string[0];
        }
    }

    public List<EntityInfo> ParseEntities(string entitiesText)
    {
        List<EntityInfo> entities = new List<EntityInfo>();

        // Define regex patterns
        string entityPattern = @"\*\*([^*]+)\*\*\s*Fields\s*(.*?)\s*(?=\*\*[^*]+|\z)";
        string fieldPattern = @"\* \*\*([^*]+)\*\*(?: \(([^\)]+)\))?";

        // Match entities using regex
        MatchCollection entityMatches = Regex.Matches(entitiesText, entityPattern, RegexOptions.Singleline);

        foreach (Match entityMatch in entityMatches)
        {
            EntityInfo entity = new EntityInfo();
            entity.Fields = new List<EntityField>();

            // Extract entity name
            entity.Name = entityMatch.Groups[1].Value.Trim();

            // Match fields using regex
            MatchCollection fieldMatches = Regex.Matches(entityMatch.Groups[2].Value, fieldPattern);

            foreach (Match fieldMatch in fieldMatches)
            {
                string fieldName = fieldMatch.Groups[1].Value.Trim();
                string fieldType = fieldMatch.Groups[2].Value.Trim();

                entity.Fields.Add(new EntityField { Name = fieldName, Type = fieldType });
            }

            entities.Add(entity);
        }

        return entities;
    }
}