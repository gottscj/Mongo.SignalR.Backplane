using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Mongo.SignalR.Backplane;

/// <summary>
/// Extension methods for configuring Mongo-based scale-out for a SignalR Server in an <see cref="ISignalRServerBuilder" />.
/// </summary>
public static class MongoBackplaneDependencyInjectionExtensions
{
    /// <summary>
    /// Adds scale-out to a <see cref="ISignalRServerBuilder"/>, using a shared Mongo DB server.
    /// </summary>
    /// <param name="signalrBuilder">The <see cref="ISignalRServerBuilder"/>.</param>
    /// <param name="mongoClient">client to use</param>
    /// <param name="configure">A callback to configure the Mongo options.</param>
    /// <returns>The same instance of the <see cref="ISignalRServerBuilder"/> for chaining.</returns>
    public static ISignalRServerBuilder AddMongoBackplane(this ISignalRServerBuilder signalrBuilder, IMongoClient mongoClient, Action<MongoOptions>? configure = null)
    {
        configure ??= (_ => { });
        signalrBuilder.Services.Configure(configure);
        signalrBuilder.Services.AddSingleton(mongoClient);
        signalrBuilder.Services.AddSingleton(new MongoHubConnectionStore());
        signalrBuilder.Services.AddSingleton<IMongoDbContext, MongoDbContext>();
        signalrBuilder.Services.AddSingleton(typeof(HubLifetimeManager<>), typeof(MongoHubLifetimeManager<>));
        return signalrBuilder;
    }
    
}
