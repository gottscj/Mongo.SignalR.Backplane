// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using NUnit.Framework;

namespace Mongo.SignalR.Backplane.Tests;

/// <summary>
/// Base test class for lifetime manager implementations. Nothing specific to scale-out for these tests.
/// </summary>
/// <typeparam name="THub">The type of the <see cref="Hub"/>.</typeparam>
[TestFixture]
[NonParallelizable]
public abstract class HubLifetimeManagerTestsBase<THub> where THub : Hub
{
    /// <summary>
    /// This API is obsolete and will be removed in a future version. Use CreateNewHubLifetimeManager in tests instead.
    /// </summary>
    [Obsolete("This API is obsolete and will be removed in a future version. Use CreateNewHubLifetimeManager in tests instead.")]
    public HubLifetimeManager<THub> Manager { get; set; }

    /// <summary>
    /// Method to create an implementation of <see cref="HubLifetimeManager{THub}"/> for use in tests.
    /// </summary>
    /// <returns>The implementation of <see cref="HubLifetimeManager{THub}"/> to test against.</returns>
    public abstract HubLifetimeManager<THub> CreateNewHubLifetimeManager();

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Test, NUnit.Framework.Category("unit")]
    public async Task SendAllAsyncWritesToAllConnectionsOutput()
    {
        using var client1 = new TestClient();
        using var client2 = new TestClient();
        var manager = CreateNewHubLifetimeManager();
        var connection1 = HubConnectionContextUtils.Create(client1.Connection);
        var connection2 = HubConnectionContextUtils.Create(client2.Connection);

        await manager.OnConnectedAsync(connection1).DefaultTimeout();
        await manager.OnConnectedAsync(connection2).DefaultTimeout();

        await manager.SendAllAsync("Hello", new object[] { "World" }).DefaultTimeout();

        var message = await client1.ReadAsync().DefaultTimeout();
        Assert.IsInstanceOf<InvocationMessage>(message);
        var invocationMessage = (InvocationMessage) message;
        Assert.AreEqual("Hello", invocationMessage.Target);
        Assert.AreEqual(invocationMessage.Arguments.Length, 1);
        Assert.AreEqual("World", (string)invocationMessage.Arguments[0]!);

        message = await client2.ReadAsync().DefaultTimeout();
        Assert.IsInstanceOf<InvocationMessage>(message);
        invocationMessage = (InvocationMessage) message;
        Assert.AreEqual("Hello", invocationMessage.Target);
        Assert.AreEqual(invocationMessage.Arguments.Length, 1);
        Assert.AreEqual("World", (string)invocationMessage.Arguments[0]!);
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Test, NUnit.Framework.Category("unit")]
    public async Task SendAllAsyncDoesNotWriteToDisconnectedConnectionsOutput()
    {
        using var client1 = new TestClient();
        using var client2 = new TestClient();
        var manager = CreateNewHubLifetimeManager();
        var connection1 = HubConnectionContextUtils.Create(client1.Connection);
        var connection2 = HubConnectionContextUtils.Create(client2.Connection);

        await manager.OnConnectedAsync(connection1).DefaultTimeout();
        await manager.OnConnectedAsync(connection2).DefaultTimeout();

        await manager.OnDisconnectedAsync(connection2).DefaultTimeout();

        await manager.SendAllAsync("Hello", new object[] { "World" }).DefaultTimeout();

        var message = await client1.ReadAsync().DefaultTimeout();
        Assert.IsInstanceOf<InvocationMessage>(message);
        var invocationMessage = (InvocationMessage) message;
        Assert.AreEqual("Hello", invocationMessage.Target);
        Assert.AreEqual(invocationMessage.Arguments.Length, 1);
        Assert.AreEqual("World", (string)invocationMessage.Arguments[0]!);

        Assert.Null(client2.TryRead());
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Test, NUnit.Framework.Category("unit")]
    public async Task SendGroupAsyncWritesToAllConnectionsInGroupOutput()
    {
        using var client1 = new TestClient();
        using var client2 = new TestClient();
        var manager = CreateNewHubLifetimeManager();
        var connection1 = HubConnectionContextUtils.Create(client1.Connection);
        var connection2 = HubConnectionContextUtils.Create(client2.Connection);

        await manager.OnConnectedAsync(connection1).DefaultTimeout();
        await manager.OnConnectedAsync(connection2).DefaultTimeout();

        await manager.AddToGroupAsync(connection1.ConnectionId, "group").DefaultTimeout();

        await manager.SendGroupAsync("group", "Hello", new object[] { "World" }).DefaultTimeout();

        var message = await client1.ReadAsync().DefaultTimeout();
        Assert.IsInstanceOf<InvocationMessage>(message);
        var invocationMessage = (InvocationMessage) message;
        Assert.AreEqual("Hello", invocationMessage.Target);
        Assert.AreEqual(invocationMessage.Arguments.Length, 1);
        Assert.AreEqual("World", (string)invocationMessage.Arguments[0]!);

        Assert.Null(client2.TryRead());
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Test, NUnit.Framework.Category("unit")]
    public async Task SendGroupExceptAsyncDoesNotWriteToExcludedConnections()
    {
        using var client1 = new TestClient();
        using var client2 = new TestClient();
        var manager = CreateNewHubLifetimeManager();
        var connection1 = HubConnectionContextUtils.Create(client1.Connection);
        var connection2 = HubConnectionContextUtils.Create(client2.Connection);

        await manager.OnConnectedAsync(connection1).DefaultTimeout();
        await manager.OnConnectedAsync(connection2).DefaultTimeout();

        await manager.AddToGroupAsync(connection1.ConnectionId, "group1").DefaultTimeout();
        await manager.AddToGroupAsync(connection2.ConnectionId, "group1").DefaultTimeout();

        await manager.SendGroupExceptAsync("group1", "Hello", new object[] { "World" }, new[] { connection2.ConnectionId }).DefaultTimeout();

        var message = await client1.ReadAsync().DefaultTimeout();
        Assert.IsInstanceOf<InvocationMessage>(message);
        var invocationMessage = (InvocationMessage) message;
        Assert.AreEqual("Hello", invocationMessage.Target);
        Assert.AreEqual(invocationMessage.Arguments.Length, 1);
        Assert.AreEqual("World", (string)invocationMessage.Arguments[0]!);

        Assert.Null(client2.TryRead());
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Test, NUnit.Framework.Category("unit")]
    public async Task SendConnectionAsyncWritesToConnectionOutput()
    {
        using var client = new TestClient();
        var manager = CreateNewHubLifetimeManager();
        var connection = HubConnectionContextUtils.Create(client.Connection);

        await manager.OnConnectedAsync(connection).DefaultTimeout();

        await manager.SendConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" }).DefaultTimeout();

        var message = await client.ReadAsync().DefaultTimeout();
        Assert.IsInstanceOf<InvocationMessage>(message);
        var invocationMessage = (InvocationMessage) message;
        Assert.AreEqual("Hello", invocationMessage.Target);
        Assert.AreEqual(invocationMessage.Arguments.Length, 1);
        Assert.AreEqual("World", (string)invocationMessage.Arguments[0]!);
    }
}
