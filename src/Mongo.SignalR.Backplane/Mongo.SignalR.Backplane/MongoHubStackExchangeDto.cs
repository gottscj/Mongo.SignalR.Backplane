using Microsoft.AspNetCore.SignalR.Protocol;
using MongoDB.Bson;

namespace Mongo.SignalR.Backplane;

public class MongoHubStackExchangeDto
{
    public ObjectId Id { get; set; }
    public ExchangeType Type { get; set; }
    public string ConnectionId { get; set; }
    public string GroupName { get; set; }
    public InvocationMessage Message { get; set; }
}