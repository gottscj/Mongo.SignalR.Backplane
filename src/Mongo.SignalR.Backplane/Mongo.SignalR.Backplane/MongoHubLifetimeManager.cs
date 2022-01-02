using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Mongo.SignalR.Backplane
{
    public class MongoHubLifetimeManager<THub> : HubLifetimeManager<THub>, IDisposable where THub : Hub
    {
        private readonly HubConnectionStore _connections = new HubConnectionStore();
        private readonly MongoSubscriptionManager _groups = new MongoSubscriptionManager();
        private readonly MongoSubscriptionManager _users = new MongoSubscriptionManager();
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1);
        private readonly ILogger _logger;
        private readonly string _serverName = GenerateServerName();
        private readonly MongoBackplaneOptions _options;
        private readonly IMongoCollection<MongoHubStackExchangeDto> _stackExchange;
        private static string GenerateServerName()
        {
            // Use the machine name for convenient diagnostics, but add a guid to make it unique.
            // Example: MyServerName_02db60e5fab243b890a847fa5c4dcb29
            return $"{Environment.MachineName}_{Guid.NewGuid():N}";
        }

        public MongoHubLifetimeManager(
            IMongoClient client,
            ILogger<MongoHubLifetimeManager<THub>> logger,
            IOptions<MongoBackplaneOptions> options)
        {
            _logger = logger;
            _options = options.Value;
            _stackExchange = client.GetDatabase(_options.DatabaseName)
                .GetCollection<MongoHubStackExchangeDto>(_options.CollectionName);
        }
        
        public void Dispose()
        {
        }

        public override Task OnConnectedAsync(HubConnectionContext connection)
        {
            connection.Features.Set<MongoFeature>(new MongoFeature());
            _connections.Add(connection);
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(HubConnectionContext connection)
        {
            _connections.Remove(connection);
            return Task.CompletedTask;
        }

        public override async Task SendAllAsync(string methodName, object[] args, CancellationToken cancellationToken = new CancellationToken())
        {
            var dto = new MongoHubStackExchangeDto
            {
                Id = ObjectId.GenerateNewId(),
                Message = new InvocationMessage(methodName, args),
                Type = ExchangeType.SendAll
            };
            await _stackExchange.InsertOneAsync(dto, new InsertOneOptions(), cancellationToken);
        }

        public override Task SendAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task SendConnectionAsync(string connectionId, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task SendGroupAsync(string groupName, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task SendGroupExceptAsync(string groupName, string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task SendUserAsync(string userId, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task AddToGroupAsync(string connectionId, string groupName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task RemoveFromGroupAsync(string connectionId, string groupName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }
    }
}