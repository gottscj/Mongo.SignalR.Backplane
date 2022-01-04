using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson.Serialization.Attributes;

namespace Mongo.SignalR.Backplane.Invocations;

[BsonDiscriminator(InvocationType.Connection)]
public class MongoInvocationConnection : MongoInvocation
{
    public MongoInvocationConnection(IEnumerable<SerializedMessage> messages, string connectionId) 
        : base(messages, new []{connectionId})
    {
    }
    
    public MongoInvocationConnection(IEnumerable<SerializedMessage> messages, IEnumerable<string>? connectionIds = null) 
        : base(messages, connectionIds)
    {
    }

    public override async Task Process(HubConnectionStore connections, MongoSubscriptionManager groups, MongoSubscriptionManager users)
    {
        var tasks = new List<Task>();
        var hubMessage = GetSerializedHubMessage();
        foreach (var connection in connections)
        {
            if (ExcludedConnectionIds?.Contains(connection.ConnectionId) == true)
            {
                continue;
            }

            if (ConnectionIds?.Contains(connection.ConnectionId) == true)
            {
                tasks.Add(connection.WriteAsync(hubMessage).AsTask());
            }
        }

        await Task.WhenAll(tasks);
    }
}