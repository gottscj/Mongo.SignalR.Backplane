using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson.Serialization.Attributes;

namespace Mongo.SignalR.Backplane.Invocations;

[BsonDiscriminator(InvocationType.Group)]
public class MongoInvocationGroup : MongoInvocation
{
    public MongoInvocationGroup(IEnumerable<SerializedMessage> messages,  IEnumerable<string>? groupNames = null) 
        : base(messages, groupNames: groupNames)
    {
    }
    public MongoInvocationGroup(IEnumerable<SerializedMessage> messages, IEnumerable<string>? excludedConnectionIds = null,  IEnumerable<string>? groupNames = null) 
        : base(messages, excludedConnectionIds, groupNames: groupNames)
    {
    }

    public override async Task Process(HubConnectionStore connections, MongoSubscriptionManager groups, MongoSubscriptionManager users)
    {
        var tasks = new List<Task>();
        var hubMessage = GetSerializedHubMessage();
        foreach (var groupName in GroupNames ?? new HashSet<string>())
        {
            foreach (var groupConnection in groups.GetConnections(groupName))
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