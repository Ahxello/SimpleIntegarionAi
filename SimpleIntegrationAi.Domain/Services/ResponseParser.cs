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

    public List<Entity> Parse(string[] lines)
    {
        var entities = new List<Entity>();
        var relationships = new List<Relationship>();

        Entity currentEntity = null;
        bool readingFields = false;
        bool readingData = false;
        string currentDataEntity = null;

        var dataSection = new Dictionary<string, List<string>>();

        foreach (var line in lines)
        {
            if (line.StartsWith("Entity: ") || line.StartsWith("Entity:"))
            {
                if (currentEntity != null)
                {
                    entities.Add(currentEntity);
                }

                currentEntity = new Entity
                {
                    Name = line.Substring(8).Trim(), Fields = new List<string>(),
                    Data = new List<Dictionary<string, string>>()
                };
                readingFields = false;
                readingData = false;
            }
            else if (line.StartsWith("Fields:") || line.StartsWith("Fields"))
            {
                readingFields = true;
                readingData = false;
            }
            else if (line.StartsWith("Relationships:") || line.StartsWith("Relationships"))
            {
                if (currentEntity != null)
                {
                    entities.Add(currentEntity);
                }

                currentEntity = null;
                readingFields = false;
                readingData = false;
            }
            else if (line.Contains(" - ") && line.Contains(": "))
            {
                var parts = line.Split(new[] { " - ", ": " }, StringSplitOptions.None);
                relationships.Add(new Relationship { From = parts[0], To = parts[1], Type = parts[2] });
            }
            else if (line.StartsWith("Детализированный анализ:") || line.StartsWith("Детализированный анализ"))
            {
                if (currentEntity != null)
                {
                    entities.Add(currentEntity);
                }

                currentEntity = null;
                readingFields = false;
                readingData = true;
            }
            else if (readingFields && !string.IsNullOrWhiteSpace(line))
            {
                currentEntity.Fields.Add(line.Trim());
            }
            else if (readingData && !string.IsNullOrWhiteSpace(line))
            {
                if (line.Trim().Contains(":"))
                {
                    var parts = line.Split(new[] { ": " }, StringSplitOptions.None);
                    currentDataEntity = parts[1].Trim();
                    if (!dataSection.ContainsKey(currentDataEntity))
                    {
                        dataSection[currentDataEntity] = new List<string>();
                    }
                }
                else if (currentDataEntity != null)
                {
                    dataSection[currentDataEntity].Add(line.Trim());
                }
            }
        }

        if (currentEntity != null)
        {
            entities.Add(currentEntity);
        }

        // Ассоциируем данные с сущностями
        foreach (var entity in entities)
        {
            if (dataSection.ContainsKey(entity.Name))
            {
                foreach (var dataLine in dataSection[entity.Name])
                {
                    var dataParts = dataLine.Split(new[] { ", " }, StringSplitOptions.None);
                    var dataDict = new Dictionary<string, string>();

                    var idDataParts = dataParts[0].Split(new[] { ": " }, StringSplitOptions.None);
                    dataDict[entity.Fields[0]] = idDataParts[0].Trim();
                    dataDict[entity.Fields[1]] = idDataParts[1].Trim();

                    for (int i = 1; i < entity.Fields.Count; i++)
                    {
                        if (i < dataParts.Length)
                        {
                            dataDict[entity.Fields[i]] = dataParts[i].Trim();
                        }
                    }

                    entity.Data.Add(dataDict);
                }
            }

        }

        return entities;
    }
}