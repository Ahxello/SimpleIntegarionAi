namespace SimpleIntegrationAi.Domain.Models;

public class MessageEntity
{
    public MessageEntity(string message)
    {
        Message = message;
    }
    public string Message { get; init; }
}