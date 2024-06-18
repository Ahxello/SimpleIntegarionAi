using System.Text.Json;
using Newtonsoft.Json;
using SimpleIntegrationAi.Domain.Models;

namespace SimpleIntegrationAi.Domain.Services;

public interface IResponseParser
{
    void Parse (string[] lines, Dictionary<string, List<string>> entities, Dictionary<string, List<string>> data, List<(string from, string to, string type)> relationships);
}