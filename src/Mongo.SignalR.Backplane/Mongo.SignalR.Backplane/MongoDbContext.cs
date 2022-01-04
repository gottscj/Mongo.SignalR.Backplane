using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Mongo.SignalR.Backplane.Invocations;
using MongoDB.Driver;

namespace Mongo.SignalR.Backplane;

public interface IMongoDbContext
{
    IMongoDatabase Database { get; }
    IMongoCollection<MongoInvocation> Invocations { get; }
    Task Add(MongoInvocation invocation, CancellationToken cancellationToken);
}

public class MongoDbContext : IMongoDbContext
{
    private readonly MongoOptions _options;

    public IMongoDatabase Database { get; }
    public IMongoCollection<MongoInvocation> Invocations => Database.GetCollection<MongoInvocation>("invocations");

    public MongoDbContext(IOptions<MongoOptions> options, IMongoClient client)
    {
        _options = options.Value;
        Database = client.GetDatabase(_options.DatabaseName);
    }

    public async Task Add(MongoInvocation invocation, CancellationToken cancellationToken)
    {
        invocation.ServerName = _options.ServerName;
        await Invocations.InsertOneAsync(invocation, new InsertOneOptions(), cancellationToken);
    }
}