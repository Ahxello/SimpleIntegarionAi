using System.Text.Json;
using Newtonsoft.Json;
using SimpleIntegrationAi.Domain.Models;

namespace SimpleIntegrationAi.Domain.Services;

public interface IResponseParser
{
    string[] GetMessageAsync(string json);
    List<EntityInfo> ParseEntities(string jsonResponse);
}