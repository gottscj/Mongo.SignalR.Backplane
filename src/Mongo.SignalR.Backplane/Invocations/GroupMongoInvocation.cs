using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson.Serialization.Attributes;

namespace Mongo.SignalR.Backplane.Invocations;

[BsonDiscriminator(InvocationType.Group)]
public class GroupMongoInvocation : MongoInvocation
{
    public GroupMongoInvocation(IEnumerable<SerializedMessage> messages,  IEnumerable<string>? groupNames = null) 
        : base(messages, groupNames: groupNames)
    {
    }
    public GroupMongoInvocation(IEnumerable<SerializedMessage> messages, IEnumerable<string>? excludedConnectionIds = null,  IEnumerable<string>? groupNames = null) 
        : base(messages, excludedConnectionIds, groupNames: groupNames)
    {
    }

    public override async Task Process(MongoHubConnectionStore connectionStore)
    {
        if(GroupNames == null || !GroupNames.Any()) return;

        var groupConnections = new List<HubConnectionContext>();
        foreach (var groupName in GroupNames)
        {
            var connections = connectionStore.GetGroupConnections(groupName, ExcludedConnectionIds);
            groupConnections.AddRange(connections);
        }

        if (!groupConnections.Any())
        {
            return;
        }
        
        var hubMessage = GetSerializedHubMessage();

        var tasks = groupConnections
            .Select(c => c.WriteAsync(hubMessage).AsTask());

        await Task.WhenAll(tasks);
    }
}