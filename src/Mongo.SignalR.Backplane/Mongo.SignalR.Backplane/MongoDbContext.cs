using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Mongo.SignalR.Backplane;

public interface IMongoDbContext
{
    IMongoDatabase Database { get; }
    IMongoCollection<MongoInvocation> Invocations { get; }
}

public class MongoDbContext : IMongoDbContext
{
    private readonly IMongoClient _client;
    private readonly MongoOptions _options;

    public IMongoDatabase Database => _client.GetDatabase(_options.DatabaseName);
    public IMongoCollection<MongoInvocation> Invocations => Database.GetCollection<MongoInvocation>(_options.CollectionName);
    
    public MongoDbContext(IOptions<MongoOptions> options, IMongoClient client)
    {
        _options = options.Value;
        _client = client;
    }
}