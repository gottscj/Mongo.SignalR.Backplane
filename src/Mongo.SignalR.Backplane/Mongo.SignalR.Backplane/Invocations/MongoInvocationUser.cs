using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson.Serialization.Attributes;

namespace Mongo.SignalR.Backplane.Invocations;
[BsonDiscriminator(InvocationType.User)]
public class MongoInvocationUser : MongoInvocation
{
    public MongoInvocationUser(IEnumerable<SerializedMessage> messages,  IEnumerable<string>? users = null) 
        : base(messages, users: users)
    {
    }

    public override async Task Process(HubConnectionStore connections, MongoSubscriptionManager groups, MongoSubscriptionManager users)
    {
        var tasks = new List<Task>();
        var hubMessage = GetSerializedHubMessage();
        foreach (var user in Users ?? new HashSet<string>())
        {
            foreach (var groupConnection in users.GetConnections(user))
            {
                if (ExcludedConnectionIds?.Contains(groupConnection.ConnectionId) == true)
                {
                    continue;
                }

                tasks.Add(groupConnection.WriteAsync(hubMessage).AsTask());
            }
        }

        await Task.WhenAll(tasks);
    }
}