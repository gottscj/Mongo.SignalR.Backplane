using Microsoft.AspNetCore.SignalR;

namespace MongoSignalR.Backplane.Sample;

public class ChatHub : Hub
{
    private readonly ILogger _logger;

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }
    
    public void SendAll(string message)
    {
        _logger.LogInformation("HUB - SendAll {Message}", message);
    }
}