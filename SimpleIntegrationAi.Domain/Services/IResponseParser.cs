using System.Text.Json;
using Newtonsoft.Json;
using SimpleIntegrationAi.Domain.Models;

namespace SimpleIntegrationAi.Domain.Services;

public interface IResponseParser
{
    List<Entity> Parse (string[] lines);
}