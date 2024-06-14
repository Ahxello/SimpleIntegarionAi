using SimpleIntegrationAi.Domain.Models;

public class ApiResponse
{
    public List<EntityInfo> Entities { get; set; }
    public List<string> Relationships { get; set; }
    public string DetailedAnalysis { get; set; }
}