using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Mongo.SignalR.Backplane.Invocations;

[BsonDiscriminator(InvocationType.Init)]
public class InitMongoInvocation : MongoInvocation
{
    public InitMongoInvocation() : base(ObjectId.GenerateNewId(),
        null,
        null,
        null,
        null,
        new List<MongoInvocationMessage>())
    {
    }
}