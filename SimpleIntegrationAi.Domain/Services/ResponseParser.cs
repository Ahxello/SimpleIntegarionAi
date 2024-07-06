using SimpleIntegrationAi.Domain.Models;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace SimpleIntegrationAi.Domain.Services;


public class ResponseParser : IResponseParser
{

    public ResponseParser() { }

    public (List<Entity>, List<Relationship>) Parse(string responseText)
    {
        string[] lines = Regex.Split(responseText, "\r\n|\r|\n");
        List<Entity> entities = new List<Entity>();
        List<Relationship> relationships = new List<Relationship>();

        Entity currentEntity = null;
        bool readingFields = false;
        bool readingData = false;
        string currentDataEntity = null;

        Dictionary<string, List<string>> dataSection = new Dictionary<string, List<string>>();

        foreach (string line in lines)
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
            else if (line.StartsWith("Fields:") || line.StartsWith("Fields") 
                || line.StartsWith("Поля") || line.StartsWith("Поля:"))
            {
                readingFields = true;
                readingData = false;
            }
            else if (line.StartsWith("Relationships:") || line.StartsWith("Relationships") 
                || line.StartsWith("Связи") || line.StartsWith("Связи:"))
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
                string[] parts = line.Split(new[] { " - ", ": " }, StringSplitOptions.None);
                try
                {
                    RelationshipType relationshipType = RelationshipTypeFromString(parts[2]);

                    string[] fromParts = parts[0].Split('.');
                    string fromTable = fromParts[0];
                    string fromField = fromParts[1];

                    string[] toParts = parts[1].Split(".");
                    string toTable = toParts[0];
                    string toField = toParts[1];

                    relationships.Add(new Relationship
                    {
                        FromTable = fromTable,
                        FromField = fromField,
                        ToTable = toTable,
                        ToField = toField,
                        Type = relationshipType
                    });
                }
                catch (ArgumentException ex)
                {
                    throw new Exception($"Unknown relationship type: {parts[2]}", ex);
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
                string field = Regex.Replace(line.Trim(), @"\s*?(?:\(.*?\)|\[.*?\]|\{.*?\})", String.Empty);
                currentEntity.Fields.Add(field);
            }

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

        foreach (Entity entity in entities)
        {
            if (dataSection.ContainsKey(entity.Name))
            {
                foreach (var dataLine in dataSection[entity.Name])
                {
                    string[] dataParts = dataLine.Split(new[] { ", " }, StringSplitOptions.None);
                    Dictionary<string, string> dataDict = new Dictionary<string, string>();

                    foreach (string dataPart in dataParts)
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
    public static RelationshipType RelationshipTypeFromString(string relationship)
    {
        switch (relationship.ToLower().Replace("-", "").Replace("_", ""))
        {
            case "onetoone":
                return RelationshipType.OneToOne;
            case "onetomany":
                return RelationshipType.OneToMany;
            case "manytoone":
                return RelationshipType.ManyToOne;
            case "manytomany":
                return RelationshipType.ManyToMany;
            case "one to one":
                return RelationshipType.OneToOne;
            case "one to many":
                return RelationshipType.OneToMany;
            case "many to one":
                return RelationshipType.ManyToOne;
            case "many to many":
                return RelationshipType.ManyToMany;
            case "one-to-one":
                return RelationshipType.OneToOne;
            case "one-to-many":
                return RelationshipType.OneToMany;
            case "many-to-one":
                return RelationshipType.ManyToOne;
            case "many-to-many":
                return RelationshipType.ManyToMany;
            default:
                throw new ArgumentException($"Unknown relationship type: {relationship}");
        }
    }
    public List<EntityGroup> ParseGroups(string responseText, List<Entity> entities)
    {
        string[] lines = Regex.Split(responseText, "\r\n|\r|\n");
        List<EntityGroup> groups = new List<EntityGroup>();
        EntityGroup currentGroup = null;
        string currentGroupName = null;

        foreach (string line in lines)
        {
            if (line.StartsWith("Group: "))
            {
                if (currentGroup != null && currentGroup.Entities.Count > 0)
                {
                    groups.Add(currentGroup);
                }

                currentGroupName = line.Substring(7).Trim();
                currentGroup = new EntityGroup
                {
                    GroupName = currentGroupName,
                    Entities = new ObservableCollection<Entity>()
                };
            }
            else if ((line.StartsWith("Entity: ") || line.StartsWith("Entity:")) && currentGroup != null)
            {
                string entityName = line.Substring(7).Trim();
                Entity entity = entities.FirstOrDefault(e => e.Name == entityName);
                if (entity != null)
                {
                    entity.GroupName = currentGroupName;
                    currentGroup.Entities.Add(entity);
                }
            }
        }

        if (currentGroup != null && currentGroup.Entities.Count > 0)
        {
            groups.Add(currentGroup);
        }

        return groups;
    }
}