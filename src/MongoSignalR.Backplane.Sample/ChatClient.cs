using System.Buffers.Text;
using System.Net;
using System.Text;
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
            var user = "user_" + _appUrl.Url.Port + ":password";
            var connection = new HubConnectionBuilder()
                .WithUrl(_appUrl + "ChatHub", options =>
                {
                    options.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(user)));
                })
                .Build();
            
            await connection.StartAsync(stoppingToken);

            connection.On<string>("NewMessage", msg 
                => _logger.LogInformation("Rcv: {Message} on {ServerAddress}", msg, _appUrl.Url));

            stoppingToken.WaitHandle.WaitOne();
        }
    }
}