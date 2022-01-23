using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mongo2Go;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;

namespace Mongo.SignalR.Backplane.Tests;

[TestFixture]
[NonParallelizable]
public class MongoHubLifetimeManagerTests : ScaleoutHubLifetimeManagerTests<IMongoClient>
{
    public class TestObject
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string TestProperty { get; set; }
    }

    private IMongoClient _mongoClient;
    private const string DatabaseName = "signalr-backplane";
    private MongoDbRunner _runner;
    
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _runner = Mongo2Go.MongoDbRunner.Start();
        _mongoClient = new MongoClient(_runner.ConnectionString);
    }
    
    [SetUp]
    public void SetUp()
    {
        _mongoClient.DropDatabase(DatabaseName);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _mongoClient.DropDatabase(DatabaseName);
        _runner.Dispose();
    }

    private static MongoHubLifetimeManager<Hub> CreateLifetimeManager(IMongoClient client,
        MessagePackHubProtocolOptions messagePackOptions = null,
        NewtonsoftJsonHubProtocolOptions jsonOptions = null)
    {
        var options = Options.Create(new MongoOptions {DatabaseName = DatabaseName});
        messagePackOptions = messagePackOptions ?? new MessagePackHubProtocolOptions();
        jsonOptions = jsonOptions ?? new NewtonsoftJsonHubProtocolOptions();
        var store = new MongoHubConnectionStore();
        var db = new MongoDbContext(options, client);
        var observer = new MongoInvocationObserver(store, db, options, NullLogger<MongoInvocationObserver>.Instance);
        var resolver = new DefaultHubProtocolResolver(new IHubProtocol[]
        {
            new NewtonsoftJsonHubProtocol(Options.Create(jsonOptions)),
            new MessagePackHubProtocol(Options.Create(messagePackOptions)),
        }, NullLogger<DefaultHubProtocolResolver>.Instance);
        
        return new MongoHubLifetimeManager<Hub>(
            store,
            observer,
            db,
            NullLogger<MongoHubLifetimeManager<Hub>>.Instance,
            resolver);
    }

    [Test, Category("unit")]
    public async Task CamelCasedJsonIsPreservedAcrossRedisBoundary()
    {
        var messagePackOptions = new MessagePackHubProtocolOptions();

        var jsonOptions = new NewtonsoftJsonHubProtocolOptions
        {
            PayloadSerializerSettings =
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }
        };

        using var client1 = new TestClient();
        using var client2 = new TestClient();
        using var manager1 = CreateLifetimeManager(_mongoClient, messagePackOptions, jsonOptions);
        using var manager2 = CreateLifetimeManager(_mongoClient);
        var connection1 = HubConnectionContextUtils.Create(client1.Connection);
        var connection2 = HubConnectionContextUtils.Create(client2.Connection);

        await manager1.OnConnectedAsync(connection1).DefaultTimeout();
        await manager2.OnConnectedAsync(connection2).DefaultTimeout();
            
        await manager1.SendAllAsync("Hello", new object[] {new TestObject {TestProperty = "Foo"}});

        var message = await client2.ReadAsync().DefaultTimeout();
        Assert.IsInstanceOf<InvocationMessage>(message);
        var invocationMessage = (InvocationMessage) message;
        Assert.AreEqual("Hello", invocationMessage.Target);
        Assert.AreEqual(invocationMessage.Arguments.Length, 1);
        Assert.IsInstanceOf<JObject>(invocationMessage.Arguments[0]);
        Assert.NotNull(invocationMessage.Arguments[0]);
        var jObject = (JObject) invocationMessage.Arguments[0]!;

        Assert.AreEqual(jObject["testProperty"]?.Value<string>(), "Foo");
    }

    public override IMongoClient CreateBackplane()
    {
        return _mongoClient;
    }

    public override MongoHubLifetimeManager<Hub> CreateNewHubLifetimeManager()
    { 
        return CreateLifetimeManager(_mongoClient);
    }

    public override MongoHubLifetimeManager<Hub> CreateNewHubLifetimeManager(IMongoClient backplane)
    {
        return CreateLifetimeManager(backplane);
    }
}