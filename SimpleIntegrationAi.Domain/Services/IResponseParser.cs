using System.Text.Json;
using Newtonsoft.Json;

namespace SimpleIntegrationAi.Domain.Services;

public interface IResponseParser
{
    string[] GetMessageAsync(string json);

}