using Microsoft.AspNetCore.SignalR;

namespace MongoSignalR.Backplane.Sample;

[BasicAuthorizationAttribute]
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

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        _logger.LogInformation("Connected: {ConnectionId}, User:{User}", Context
            .ConnectionId, string.Join(",", Context.User?.Claims.Select(c => $"{c.Type}:{c.Value}") ?? new List<string>()));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
        _logger.LogInformation("Disconnected: {ConnectionId}", Context.ConnectionId);
   
    }
}