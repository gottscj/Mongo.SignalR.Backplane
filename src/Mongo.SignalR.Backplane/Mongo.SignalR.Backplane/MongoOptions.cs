using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace Mongo.SignalR.Backplane;

public class MongoOptions
{
    public string DatabaseName { get; set; } = "signalr";
    public string CollectionName { get; set; } = "stack-exchange";

    private IMongoCollection<MongoInvocation> _stackExchange;
    private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1);

}