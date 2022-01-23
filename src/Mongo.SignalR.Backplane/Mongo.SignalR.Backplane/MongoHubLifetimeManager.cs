using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mongo.SignalR.Backplane.Invocations;

namespace Mongo.SignalR.Backplane
{
    public class MongoHubLifetimeManager<THub> : HubLifetimeManager<THub>, IDisposable where THub : Hub
    {
        private readonly MongoHubConnectionStore _connections;
        private readonly MongoInvocationObserver _observer;

        private readonly IMongoDbContext _db;
        private readonly ILogger _logger;

        private readonly DefaultHubMessageSerializer _messageSerializer;
        private readonly Task _observeTask;

        public MongoHubLifetimeManager(
            MongoHubConnectionStore connections,
            MongoInvocationObserver observer,
            IMongoDbContext db,
            ILogger<MongoHubLifetimeManager<THub>> logger,
            IHubProtocolResolver hubProtocolResolver,
            IOptions<HubOptions>? globalHubOptions = null,
            IOptions<HubOptions<THub>>? hubOptions = null)
        {
            _connections = connections;
            _observer = observer;
            _db = db;
            _logger = logger;
            if (globalHubOptions != null && hubOptions != null)
            {
                _messageSerializer = new DefaultHubMessageSerializer(hubProtocolResolver,
                    globalHubOptions.Value.SupportedProtocols, hubOptions.Value.SupportedProtocols);
            }
            else
            {
                var supportedProtocols = hubProtocolResolver.AllProtocols.Select(p => p.Name).ToList();
                _messageSerializer = new DefaultHubMessageSerializer(hubProtocolResolver, supportedProtocols, null);
            }

            _observeTask = _observer.ExecuteAsync(HandleInvocationMessage);
        }

        public override Task OnConnectedAsync(HubConnectionContext connection)
        {
            _connections.Add(connection);
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(HubConnectionContext connection)
        {
            _connections.Remove(connection);
            return Task.CompletedTask;
        }

        public override async Task SendAllAsync(string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var messages = GetMessages(methodName, args);
            await _db.Insert(MongoInvocation.SendAll(messages), cancellationToken);
        }

        public override async Task SendAllExceptAsync(string methodName, object[] args,
            IReadOnlyList<string>? excludedConnectionIds,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var messages = GetMessages(methodName, args);
            await _db.Insert(MongoInvocation.SendAll(messages, excludedConnectionIds), cancellationToken);
        }

        public override async Task SendConnectionAsync(string connectionId, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            // If the connection is local we can skip sending the message through the bus since we require sticky connections.
            // This also saves serializing and deserializing the message!
            var connection = _connections[connectionId];
            if (connection != null)
            {
                await connection.WriteAsync(new InvocationMessage(methodName, args), cancellationToken).AsTask();
                return;
            }

            var messages = GetMessages(methodName, args);
            await _db.Insert(MongoInvocation.Connection(messages, connectionId), cancellationToken);
        }

        public override async Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName,
            object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var messages = GetMessages(methodName, args);
            await _db.Insert(MongoInvocation.Connection(messages, connectionIds), cancellationToken);
        }

        public override async Task SendGroupAsync(string groupName, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var messages = GetMessages(methodName, args);
            await _db.Insert(MongoInvocation.SendGroup(messages, groupName), cancellationToken);
        }

        public override async Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var messages = GetMessages(methodName, args);
            await _db.Insert(MongoInvocation.SendGroup(messages, groupNames), cancellationToken);
        }

        public override async Task SendGroupExceptAsync(string groupName, string methodName, object[] args,
            IReadOnlyList<string> excludedConnectionIds,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var messages = GetMessages(methodName, args);
            await _db.Insert(MongoInvocation.SendGroup(messages, groupName, excludedConnectionIds), cancellationToken);
        }

        public override async Task SendUserAsync(string userId, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var messages = GetMessages(methodName, args);
            await _db.Insert(MongoInvocation.SendUser(messages, userId), cancellationToken);
        }

        public override async Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var messages = GetMessages(methodName, args);
            await _db.Insert(MongoInvocation.SendUsers(messages, userIds), cancellationToken);
        }

        public override async Task AddToGroupAsync(string connectionId, string groupName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (_connections.AddToGroup(connectionId, groupName))
            {
                // short circuit if connection is on this server

                return;
            }

            await _db.Insert(MongoInvocation.AddToGroup(connectionId, groupName), cancellationToken);
        }


        public override async Task RemoveFromGroupAsync(string connectionId, string groupName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var connection = _connections[connectionId];
            if (connection != null)
            {
                // short circuit if connection is on this server
                _connections.RemoveFromGroup(connectionId, groupName);
                return;
            }

            await _db.Insert(MongoInvocation.RemoveFromGroup(connectionId, groupName), cancellationToken);
        }

        private IEnumerable<SerializedMessage> GetMessages(string methodName, object[]? args)
        {
            var messages = _messageSerializer.SerializeMessage(new InvocationMessage(methodName, args));
            return messages;
        }

        private async Task HandleInvocationMessage(MongoInvocation invocation)
        {
            if (invocation is AddToGroupMongoInvocation)
            {
                _connections.AddToGroup(invocation);
                return;
            }

            if (invocation is RemoveFromGroupMongoInvocation)
            {
                _connections.RemoveFromGroup(invocation);
                return;
            }

            await invocation.Process(_connections);
        }

        public void Dispose()
        {
            _observer.Dispose();
            _observeTask.Wait(1000);
        }
    }
}