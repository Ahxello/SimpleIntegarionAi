using SimpleIntegrationAi.Domain.Models;
using System.Text.RegularExpressions;

namespace SimpleIntegrationAi.Domain.Services;


public class ResponseParser : IResponseParser
{

    public ResponseParser()
    {
    }

    public (List<Entity>, List<Relationship>) Parse(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        var entities = new List<Entity>();
        var relationships = new List<Relationship>();

        Entity currentEntity = null;
        bool readingFields = false;
        bool readingData = false;
        string currentDataEntity = null;

        var dataSection = new Dictionary<string, List<string>>();

        foreach (var line in lines)
        {
            if ((line.StartsWith("Entity: ") || line.StartsWith("Entity:"))  && readingData == false)
            {
                if (currentEntity != null && currentEntity.Fields.Count != 0)
                {
                    entities.Add(currentEntity);
                }

                currentEntity = new Entity
                {
                    Name = line.Substring(7).Trim(), Fields = new List<string>(),
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
                if (currentEntity != null & currentEntity.Fields.Count != 0)
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
                if (Enum.TryParse(parts[2], out RelationshipType relationshipType))
                {
                    string[] fromParts = parts[0].Split('.');
                    string fromTable = fromParts[0];
                    string fromField = fromParts[1];

                    string[] toParts = parts[1].Split(".");
                    string toTable = toParts[0];
                    string toField = toParts[1];

                    relationships.Add(new Relationship { 
                        FromTable = fromTable, 
                        FromField = fromField, 
                        ToTable = toTable,
                        ToField = toField, 
                        Type = relationshipType });
                }
                else
                {
                    throw new Exception($"Unknown relationship type: {parts[2]}");
                }
            }
            else if (line.StartsWith("Детализированный анализ:") || line.StartsWith("Детализированный анализ") 
                || line.StartsWith("Detailed Analysis") || line.StartsWith("Detailed Analysis:"))
            {
                if (currentEntity != null && currentEntity.Fields.Count != 0)
                {
                    entities.Add(currentEntity);
                }

                currentEntity = null;
                readingFields = false;
                readingData = true;
            }
            else if (readingFields && !string.IsNullOrWhiteSpace(line))
            {
                var field = Regex.Replace(line.Trim(), @"\s*?(?:\(.*?\)|\[.*?\]|\{.*?\})", String.Empty);
                currentEntity.Fields.Add(field);
            }
            //Need Fix
            else if (readingData && !string.IsNullOrWhiteSpace(line))
            {
                if (line.Trim().StartsWith("Entity:") || line.Trim().StartsWith("Entity: "))
                {
                    currentDataEntity = line.Substring(7).Trim();
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

        if (currentEntity != null && currentEntity.Fields.Count != 0)
        {
            entities.Add(currentEntity);
        }

        foreach (var entity in entities)
        {
            if (dataSection.ContainsKey(entity.Name))
            {
                foreach (var dataLine in dataSection[entity.Name])
                {
                    var dataParts = dataLine.Split(new[] { ", " }, StringSplitOptions.None);
                    var dataDict = new Dictionary<string, string>();

                    foreach (var dataPart in dataParts)
                    {
                        var keyValue = dataPart.Split(new[] { ": " }, StringSplitOptions.None);
                        if (keyValue.Length == 2)
                        {
                            dataDict[keyValue[0].Trim()] = keyValue[1].Trim().Trim('"');
                        }
                    }

                    entity.Data.Add(dataDict);
                }
            }
        }

        return (entities, relationships);
    }
}