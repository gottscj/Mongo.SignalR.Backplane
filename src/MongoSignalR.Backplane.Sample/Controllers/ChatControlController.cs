using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace MongoSignalR.Backplane.Sample.Controllers;

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

    [HttpPost(Name = "SendAll")]
    public async Task<IActionResult> Send(string message)
    {
        _logger.LogInformation("CONTROLLER - SendAll - {Message}", message);
        await _hubContext.Clients.All.SendAsync("SendAll", message);

        return Ok();
    }
}
