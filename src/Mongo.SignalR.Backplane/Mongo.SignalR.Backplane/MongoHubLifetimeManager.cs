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
        private readonly HubConnectionStore _connections = new();
        private readonly MongoSubscriptionManager _groups = new();
        private readonly MongoSubscriptionManager _users = new();

        private readonly IMongoDbContext _db;
        private readonly ILogger _logger;

        private readonly DefaultHubMessageSerializer _messageSerializer;
        private readonly IDisposable _subscription;

        public MongoHubLifetimeManager(
            IMongoDbContext db,
            IMongoObserver observer,
            ILogger<MongoHubLifetimeManager<THub>> logger,
            IHubProtocolResolver hubProtocolResolver)
            : this(db, observer, logger, hubProtocolResolver, globalHubOptions: null, hubOptions: null)
        {
        }

        public MongoHubLifetimeManager(
            IMongoDbContext db,
            IMongoObserver observer,
            ILogger<MongoHubLifetimeManager<THub>> logger,
            IHubProtocolResolver hubProtocolResolver,
            IOptions<HubOptions>? globalHubOptions,
            IOptions<HubOptions<THub>>? hubOptions)
        {
            _db = db;
            _logger = logger;

            _subscription = observer.Subscribe(OnInvocationMessage);

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
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }


        public override Task OnConnectedAsync(HubConnectionContext connection)
        {
            connection.Features.Set(new MongoFeature());
            _connections.Add(connection);
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(HubConnectionContext connection)
        {
            _connections.Remove(connection);
            var feature = connection.Features.Get<MongoFeature>()!;
            var groupNames = feature.Groups;

            // Copy the groups to an array here because they get removed from this collection
            // in RemoveFromGroup
            foreach (var group in groupNames.ToArray())
            {
                RemoveFromGroup(connection, @group);
            }

            return Task.CompletedTask;
        }

        public override async Task SendAllAsync(string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var messages = GetMessages(methodName, args);
            await _db.Add(MongoInvocation.All(messages), cancellationToken);
        }

        public override async Task SendAllExceptAsync(string methodName, object[] args,
            IReadOnlyList<string>? excludedConnectionIds,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var messages = GetMessages(methodName, args);
            await _db.Add(MongoInvocation.All(messages, excludedConnectionIds), cancellationToken);
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
            await _db.Add(MongoInvocation.Connection(messages, connectionId), cancellationToken);
        }

        public override async Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName,
            object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var messages = GetMessages(methodName, args);
            await _db.Add(MongoInvocation.Connection(messages, connectionIds), cancellationToken);
        }

        public override async Task SendGroupAsync(string groupName, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var messages = GetMessages(methodName, args);
            await _db.Add(MongoInvocation.Group(messages, groupName), cancellationToken);
        }

        public override async Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var messages = GetMessages(methodName, args);
            await _db.Add(MongoInvocation.Group(messages, groupNames), cancellationToken);
        }

        public override async Task SendGroupExceptAsync(string groupName, string methodName, object[] args,
            IReadOnlyList<string> excludedConnectionIds,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var messages = GetMessages(methodName, args);
            await _db.Add(MongoInvocation.Group(messages, groupName, excludedConnectionIds), cancellationToken);
        }

        public override async Task SendUserAsync(string userId, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var messages = GetMessages(methodName, args);
            await _db.Add(MongoInvocation.User(messages, userId), cancellationToken);
        }

        public override async Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var messages = GetMessages(methodName, args);
            await _db.Add(MongoInvocation.User(messages, userIds), cancellationToken);
        }

        public override async Task AddToGroupAsync(string connectionId, string groupName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var connection = _connections[connectionId];
            if (connection != null)
            {
                // short circuit if connection is on this server
                AddToGroup(connection, groupName);
                return;
            }

            await _db.Add(MongoInvocation.AddToGroup(connectionId, groupName), cancellationToken);
        }


        public override async Task RemoveFromGroupAsync(string connectionId, string groupName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var connection = _connections[connectionId];
            if (connection != null)
            {
                // short circuit if connection is on this server
                RemoveFromGroup(connection, groupName);
                return;
            }

            await _db.Add(MongoInvocation.RemoveFromGroup(connectionId, groupName), cancellationToken);
        }

        private IEnumerable<SerializedMessage> GetMessages(string methodName, object[]? args)
        {
            var messages = _messageSerializer.SerializeMessage(new InvocationMessage(methodName, args));
            return messages;
        }

        private async Task OnInvocationMessage(MongoInvocation invocation)
        {
            switch (invocation)
            {
                case MongoInvocationAddToGroup:
                    invocation.ProcessGroupAction(_connections, AddToGroup);
                    return;
                case MongoInvocationRemoveFromGroup:
                    invocation.ProcessGroupAction(_connections, RemoveFromGroup);
                    return;
            }

            await invocation.Process(_connections, _groups, _users);
        }

        private static void AddToGroup(HubConnectionContext connection, string groupName)
        {
            var feature = connection.Features.Get<MongoFeature>()!;
            var groupNames = feature.Groups;

            lock (groupNames)
            {
                groupNames.Add(groupName);
            }
        }

        private static void RemoveFromGroup(HubConnectionContext connection, string groupName)
        {
            var feature = connection.Features.Get<MongoFeature>()!;
            var groupNames = feature.Groups;
            lock (groupNames)
            {
                groupNames.Remove(groupName);
            }
        }
    }
}