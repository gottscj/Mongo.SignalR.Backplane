using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Mongo.SignalR.Backplane.Invocations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Mongo.SignalR.Backplane;

public interface IMongoDbContext
{
    IMongoDatabase Database { get; }
    IMongoCollection<MongoInvocation> Invocations { get; }
    Task Insert(MongoInvocation invocation, CancellationToken cancellationToken);
}

public class MongoDbContext : IMongoDbContext
{
    private readonly MongoOptions _options;

    public IMongoDatabase Database { get; }

    public IMongoCollection<MongoInvocation> Invocations =>
        Database.GetCollection<MongoInvocation>(_options.CollectionName);

    public MongoDbContext(IOptions<MongoOptions> options, IMongoClient client)
    {
        _options = options.Value;
        if (string.IsNullOrEmpty(_options.DatabaseName))
        {
            throw new ArgumentException("'DatabaseName' must have a value");
        }

        if (string.IsNullOrEmpty(_options.CollectionName))
        {
            throw new ArgumentException("'CollectionName' must have a value");
        }

        Database = client.GetDatabase(_options.DatabaseName);
        if (!CollectionExists(Database, _options.CollectionName))
        {
            Database.CreateCollection(_options.CollectionName, new CreateCollectionOptions
            {
                Capped = true,
                MaxSize = _options.MaxSize * 1024 * 1024
            });
            Invocations.FindOneAndUpdate(new BsonDocument("_t", InvocationType.Init), new BsonDocument
                {
                    ["$setOnInsert"] = new BsonDocument
                    {
                        ["_t"] = InvocationType.Init
                    }
                },
                new FindOneAndUpdateOptions<MongoInvocation>
                {
                    IsUpsert = true,
                    Sort = Builders<MongoInvocation>.Sort.Descending(j => j.Id),
                    ReturnDocument = ReturnDocument.After
                });
        }
    }

    private bool CollectionExists(IMongoDatabase database, string collectionName)
    {
        var filter = new BsonDocument("name", collectionName);
        var options = new ListCollectionNamesOptions {Filter = filter};

        return database.ListCollectionNames(options).Any();
    }

    public async Task Insert(MongoInvocation invocation, CancellationToken cancellationToken)
    {
        invocation.ServerName = _options.ServerName;
        await Invocations.InsertOneAsync(invocation, new InsertOneOptions(), cancellationToken);
    }
}