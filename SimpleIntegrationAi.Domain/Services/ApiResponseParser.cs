using Newtonsoft.Json;
using SimpleIntegrationAi.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimpleIntegrationAi.Domain.Services
{
    public static class ApiResponseParser
    {
        public static Dictionary<string, List<Dictionary<string, string>>> ParseText(string inputText)
        {
            var parsedData = new Dictionary<string, List<Dictionary<string, string>>>();

            // Регулярное выражение для извлечения блоков данных
            string pattern = @"(\S+)\s+\((.*?)\)\s+([\s\S]*?)(?=\S+\s+\(|$)";

            Regex regex = new Regex(pattern);
            MatchCollection matches = regex.Matches(inputText);

            foreach (Match match in matches)
            {
                string entityName = match.Groups[1].Value.Trim();
                string entityType = match.Groups[2].Value.Trim();
                string entityData = match.Groups[3].Value.Trim();

                if (!parsedData.ContainsKey(entityName))
                {
                    parsedData[entityName] = new List<Dictionary<string, string>>();
                }

                var attributes = ParseAttributes(entityData);
                parsedData[entityName].Add(attributes);
            }

            return parsedData;
        }

        public static Dictionary<string, string> ParseAttributes(string entityData)
        {
            var attributes = new Dictionary<string, string>();

            string[] lines = entityData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                int colonIndex = line.IndexOf(':');
                if (colonIndex != -1)
                {
                    string attributeName = line.Substring(0, colonIndex).Trim();
                    string attributeValue = line.Substring(colonIndex + 1).Trim();
                    attributes[attributeName] = attributeValue;
                }
            }

            return attributes;
        }


    }

}
