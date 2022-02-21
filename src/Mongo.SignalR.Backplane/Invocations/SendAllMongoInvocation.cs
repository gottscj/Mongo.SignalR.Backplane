using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson.Serialization.Attributes;

namespace Mongo.SignalR.Backplane.Invocations;

[BsonDiscriminator(InvocationType.All)]
public class SendAllMongoInvocation : MongoInvocation
{
    public SendAllMongoInvocation(
        IEnumerable<SerializedMessage> messages, 
        IEnumerable<string>? excludedConnectionIds = null) 
        : base(messages, excludedConnectionIds)
    {
    }

    public override async Task Process(MongoHubConnectionStore connections)
    {
        var hubMessage = GetSerializedHubMessage();
        var tasks = new List<Task>();
        foreach (var connection in connections)
        {
            if (ExcludedConnectionIds?.Contains(connection.ConnectionId) == true)
            {
                continue;
            }

            tasks.Add(connection.WriteAsync(hubMessage).AsTask());
        }

        await Task.WhenAll(tasks);
    }
}