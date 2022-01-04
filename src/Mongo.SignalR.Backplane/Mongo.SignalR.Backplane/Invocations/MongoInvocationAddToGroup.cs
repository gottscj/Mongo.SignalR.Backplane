using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson.Serialization.Attributes;

namespace Mongo.SignalR.Backplane.Invocations;

[BsonDiscriminator(InvocationType.AddToGroup)]
public class MongoInvocationAddToGroup : MongoInvocation
{
    public MongoInvocationAddToGroup(string connectionId, string group)
        : base(new List<SerializedMessage>(), connectionIds: new []{connectionId}, groupNames: new []{group})
    {
    }
    
    public (string connectionId, string groupName) GetGroupAndConnectionId()
    {
        var connectionId = ConnectionIds?.First() ?? "";
        var groupName = GroupNames?.First() ?? "";
        return (connectionId, groupName);
    }

    public override void ProcessGroupAction(HubConnectionStore connections, Action<HubConnectionContext, string> groupAction)
    {
        var connectionId = ConnectionIds?.First() ?? "";
        var groupName = GroupNames?.First() ?? "";
        var connection = connections[connectionId];
        if (connection != null)
        {
            groupAction?.Invoke(connection, groupName);
        }
    }
}