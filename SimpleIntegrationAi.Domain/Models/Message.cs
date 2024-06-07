namespace SimpleIntegrationAi.Domain.Models;

public class MessageItem
{
    public MessageItem(string message)
    {
        Message = message;
    }
    public string Message { get; init; }
}