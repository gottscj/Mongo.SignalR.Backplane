using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace MongoSignalR.Backplane.Sample.Controllers;

[BasicAuthorizationAttribute]
[ApiController]
[Route("[controller]")]
public class ChatControlController : ControllerBase
{
    private readonly ILogger<ChatControlController> _logger;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatControlController(ILogger<ChatControlController> logger, IHubContext<ChatHub> hubContext)
    {
        _logger = logger;
        _hubContext = hubContext;
    }

    [HttpPost("send-all", Name = "SendAll")]
    public async Task<IActionResult> Send(string message)
    {
        _logger.LogInformation("CONTROLLER - SendAll - {Message}", message);
        await _hubContext.Clients.All.SendAsync("NewMessage", message);

        return Ok();
    }
    
    [HttpPost("send-group", Name = "SendGroup")]
    public async Task<IActionResult> SendGroup(string group, string message)
    {
        _logger.LogInformation("CONTROLLER - SendGroup - {Group}:{Message}", group, message);
        await _hubContext.Clients.Group(group).SendAsync("NewMessage", message);

        return Ok();
    }
    
    [HttpPost("add-to-group", Name = "AddToGroup")]
    public async Task<IActionResult> AddToGroup(string connectionId, string group)
    {
        _logger.LogInformation("CONTROLLER - AddToGroup - {ConnectionId}:{Group}", connectionId, group);
        await _hubContext.Groups.AddToGroupAsync(connectionId, group);

        return Ok();
    }
    
    [HttpPost("remove-from-group", Name = "RemoveFromGroup")]
    public async Task<IActionResult> RemoveFromGroup(string connectionId, string group)
    {
        _logger.LogInformation("CONTROLLER - RemoveFromGroup - {ConnectionId}:{Group}", connectionId, group);
        await _hubContext.Groups.RemoveFromGroupAsync(connectionId, group);

        return Ok();
    }
    
    [HttpPost("send-connection", Name = "SendConnection")]
    public async Task<IActionResult> SendConnection(string connectionId, string message)
    {
        _logger.LogInformation("CONTROLLER - SendConnection - {ConnectionId}:{Message}", connectionId, message);
        await _hubContext.Clients.Client(connectionId).SendAsync("NewMessage", message);

        return Ok();
    }
    
    [HttpPost("send-user", Name = "SendUser")]
    public async Task<IActionResult> SendUser(string user, string message)
    {
        _logger.LogInformation("CONTROLLER - SendUser - {User}:{Message}", user, message);
        await _hubContext.Clients.User(user).SendAsync("NewMessage", message);

        return Ok();
    }
}
