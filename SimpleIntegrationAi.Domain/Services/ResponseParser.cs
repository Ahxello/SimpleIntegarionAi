using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SimpleIntegrationAi.Domain.Services;

public class ResponseParser : IResponseParser
{
    public string[] GetMessageAsync(string json)
    {
        try
        {
            JObject parsedJson = JObject.Parse(json);
            string text = parsedJson["result"]["alternatives"][0]["message"]["text"].ToString();

            return text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries); ;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Ошибка при парсинге JSON: {ex.Message}");
            return new string[0];
        }
    }
}