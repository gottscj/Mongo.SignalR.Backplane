using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson.Serialization.Attributes;

namespace Mongo.SignalR.Backplane.Invocations;
[BsonDiscriminator(InvocationType.User)]
public class UserMongoInvocation : MongoInvocation
{
    public UserMongoInvocation(IEnumerable<SerializedMessage> messages,  IEnumerable<string>? users = null) 
        : base(messages, users: users)
    {
    }

    public override async Task Process(MongoHubConnectionStore connectionStore)
    {
        if(Users == null || !Users.Any()) return;

        var userConnections = new List<HubConnectionContext>();
        foreach (var user in Users)
        {
            var connections = connectionStore.GetUserConnections(user, ExcludedConnectionIds);
            userConnections.AddRange(connections);
        }

        if (!userConnections.Any())
        {
            return;
        }
        
        var hubMessage = GetSerializedHubMessage();

        var tasks = userConnections
            .Select(c => c.WriteAsync(hubMessage).AsTask());

        await Task.WhenAll(tasks);
    }
}