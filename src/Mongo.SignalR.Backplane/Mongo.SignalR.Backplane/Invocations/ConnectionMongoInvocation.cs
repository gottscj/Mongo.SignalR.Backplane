using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson.Serialization.Attributes;

namespace Mongo.SignalR.Backplane.Invocations;

[BsonDiscriminator(InvocationType.Connection)]
public class ConnectionMongoInvocation : MongoInvocation
{
    public ConnectionMongoInvocation(IEnumerable<SerializedMessage> messages, string connectionId)
        : base(messages, connectionIds: new[] {connectionId})
    {
    }

    public ConnectionMongoInvocation(IEnumerable<SerializedMessage> messages, IEnumerable<string>? connectionIds = null)
        : base(messages, connectionIds: connectionIds)
    {
    }

    public override async Task Process(MongoHubConnectionStore connections)
    {
        var tasks = new List<Task>();

        if (ConnectionIds == null)
        {
            return;
        }

        var hubMessage = GetSerializedHubMessage();
        foreach (var connectionId in ConnectionIds)
        {
            var connection = connections[connectionId];
            if (connection == null)
            {
                continue;
            }
            
            if (ExcludedConnectionIds?.Contains(connection.ConnectionId) != true)
            {
                tasks.Add(connection.WriteAsync(hubMessage).AsTask());
            }
        }

        await Task.WhenAll(tasks);
    }
}