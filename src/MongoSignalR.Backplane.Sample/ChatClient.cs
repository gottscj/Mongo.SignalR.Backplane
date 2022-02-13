using Microsoft.AspNetCore.SignalR.Client;

namespace MongoSignalR.Backplane.Sample
{
    public class ChatClient : BackgroundService
    {
        private readonly AppUrl _appUrl;
        private readonly ILogger<ChatClient> _logger;
        public ChatClient(AppUrl appUrl, ILogger<ChatClient> logger)
        {
            _appUrl = appUrl;
            _logger = logger;

        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(1000, stoppingToken);
            var connection = new HubConnectionBuilder()
                .WithUrl(_appUrl + "/ChatHub")
                .Build();
            await connection.StartAsync(stoppingToken);
            
            connection.On<string>("NewMessage", msg 
                => _logger.LogInformation("Rcv: {Message} on {ServerAddress}", msg, _appUrl.Url));

            stoppingToken.WaitHandle.WaitOne();
        }
    }
}