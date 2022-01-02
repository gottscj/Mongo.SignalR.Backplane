using MongoDB.Bson;

namespace Mongo.SignalR.Backplane;

public class MethodArg
{
    public BsonType Type { get; set; }
    public object Value { get; set; }
}