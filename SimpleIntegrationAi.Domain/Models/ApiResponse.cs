using SimpleIntegrationAi.Domain.Models;

public class ApiResponse
{
    public List<Entity> Entities { get; set; }
    public List<string> Relationships { get; set; }
    public string DetailedAnalysis { get; set; }
}