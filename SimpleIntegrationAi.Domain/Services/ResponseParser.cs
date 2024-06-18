using SimpleIntegrationAi.Domain.Models;
using System.Text.RegularExpressions;

namespace SimpleIntegrationAi.Domain.Services;

public enum Cardinality
{
    OneToOne,
    OneToMany,
    ManyToOne,
    ManyToMany
}

public class ResponseParser : IResponseParser
{

    public ResponseParser()
    {
    }

    public void Parse(string[] lines, Dictionary<string, List<string>> entities, Dictionary<string, List<string>> data, List<(string from, string to, string type)> relationships)
    {
        string currentEntity = null;
        bool parsingFields = false, parsingData = false;

        foreach (var line in lines)
        {
            if (line.StartsWith("Entity:"))
            {
                currentEntity = line.Split(':')[1].Trim();
                entities[currentEntity] = new List<string>{ }; // Primary key for all entities
                data[currentEntity] = new List<string>();
                parsingFields = false;
                parsingData = false;
            }
            else if (line.StartsWith("Fields"))
            {
                parsingFields = true;
                parsingData = false;
            }
            else if (line.StartsWith("Relationships:"))
            {
                parsingFields = false;
                parsingData = false;
            }
            else if (line.Contains("-") && line.Contains(":"))
            {
                var parts = line.Split(new[] { '-', ':' }, StringSplitOptions.RemoveEmptyEntries);
                relationships.Add((parts[0].Trim(), parts[1].Trim(), parts[2].Trim()));
            }
            else if (line.StartsWith("Детализированный анализ"))
            {
                parsingFields = false;
                parsingData = true;
            }
            else if (!string.IsNullOrWhiteSpace(line))
            {
                if (parsingFields && currentEntity != null)
                {
                    entities[currentEntity].Add(line.Trim());
                }
                else if (parsingData && currentEntity != null)
                {
                    data[currentEntity].Add(line.Trim());
                }
            }
        }
    }
}