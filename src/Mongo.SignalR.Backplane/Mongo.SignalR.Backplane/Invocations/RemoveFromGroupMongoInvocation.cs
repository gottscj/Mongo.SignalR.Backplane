using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson.Serialization.Attributes;

namespace Mongo.SignalR.Backplane.Invocations;

[BsonDiscriminator(InvocationType.RemoveFromGroup)]
public class RemoveFromGroupMongoInvocation : MongoInvocation
{
    public RemoveFromGroupMongoInvocation(string connectionId, string group)
        : base(new List<SerializedMessage>(), connectionIds: new []{connectionId}, groupNames: new []{group})
    {
    }
}