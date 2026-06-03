using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;

namespace Mongo.SignalR.Backplane;

/// <summary>
/// Extension methods for configuring Mongo-based scale-out for a SignalR Server in an <see cref="ISignalRServerBuilder" />.
/// </summary>
public static class MongoBackplaneDependencyInjectionExtensions
{
    /// <summary>
    /// Adds scale-out to a <see cref="ISignalRServerBuilder"/>, using a shared Mongo DB server.
    /// Uses an <see cref="IMongoClient"/> that is already registered in the DI container.
    /// </summary>
    /// <param name="signalrBuilder">The <see cref="ISignalRServerBuilder"/>.</param>
    /// <param name="configure">A callback to configure the Mongo options.</param>
    /// <returns>The same instance of the <see cref="ISignalRServerBuilder"/> for chaining.</returns>
    public static ISignalRServerBuilder AddMongoBackplane(this ISignalRServerBuilder signalrBuilder,
        Action<MongoOptions>? configure = null)
    {
        configure ??= _ => { };
        
        return signalrBuilder.AddMongoBackplane((_, options) => configure(options));
    }
    
    /// <summary>
    /// Adds scale-out to a <see cref="ISignalRServerBuilder"/>, using a shared Mongo DB server.
    /// Registers <paramref name="mongoClient"/> via <see cref="ServiceCollectionDescriptorExtensions.TryAddSingleton{TService}(IServiceCollection, TService)"/>
    /// so an already-registered <see cref="IMongoClient"/> in the DI container is respected.
    /// </summary>
    /// <param name="signalrBuilder">The <see cref="ISignalRServerBuilder"/>.</param>
    /// <param name="mongoClient">The <see cref="IMongoClient"/> to use.</param>
    /// <param name="configure">A callback to configure the Mongo options.</param>
    /// <returns>The same instance of the <see cref="ISignalRServerBuilder"/> for chaining.</returns>
    public static ISignalRServerBuilder AddMongoBackplane(this ISignalRServerBuilder signalrBuilder,
        IMongoClient mongoClient, Action<MongoOptions>? configure = null)
    {
        configure ??= _ => { };

        return signalrBuilder.AddMongoBackplane(mongoClient, (_, options) => configure(options));
    }

    /// <summary>
    /// Adds scale-out to a <see cref="ISignalRServerBuilder"/>, using a shared Mongo DB server.
    /// Registers <paramref name="mongoClient"/> via <see cref="ServiceCollectionDescriptorExtensions.TryAddSingleton{TService}(IServiceCollection, TService)"/>
    /// so an already-registered <see cref="IMongoClient"/> in the DI container is respected.
    /// </summary>
    /// <param name="signalrBuilder">The <see cref="ISignalRServerBuilder"/>.</param>
    /// <param name="mongoClient">The <see cref="IMongoClient"/> to use.</param>
    /// <param name="configure">A callback to configure the Mongo options.</param>
    /// <returns>The same instance of the <see cref="ISignalRServerBuilder"/> for chaining.</returns>
    public static ISignalRServerBuilder AddMongoBackplane(this ISignalRServerBuilder signalrBuilder,
        IMongoClient mongoClient, Action<IServiceProvider, MongoOptions> configure)
    {
        signalrBuilder.Services.TryAddSingleton(mongoClient);
        return signalrBuilder.AddMongoBackplane(configure);
    }

    /// <summary>
    /// Adds scale-out to a <see cref="ISignalRServerBuilder"/>, using a shared MongoDB server.
    /// </summary>
    /// <param name="signalrBuilder">The <see cref="ISignalRServerBuilder"/>.</param>
    /// <param name="configure">A callback to configure the Mongo options.</param>
    /// <returns>The same instance of the <see cref="ISignalRServerBuilder"/> for chaining.</returns>
    public static ISignalRServerBuilder AddMongoBackplane(this ISignalRServerBuilder signalrBuilder,
        Action<IServiceProvider, MongoOptions> configure)
    {
        signalrBuilder.Services.AddOptions<MongoOptions>()
            .Configure<IServiceProvider>((options, serviceProvider) =>
            {
                configure(serviceProvider, options);
            });

        signalrBuilder.Services.AddSingleton(new MongoHubConnectionStore());
        signalrBuilder.Services.AddSingleton<IMongoDbContext, MongoDbContext>();
        signalrBuilder.Services.AddHostedService<MongoInvocationObserver>();
        signalrBuilder.Services.AddSingleton(typeof(HubLifetimeManager<>), typeof(MongoHubLifetimeManager<>));
        return signalrBuilder;
    }
}