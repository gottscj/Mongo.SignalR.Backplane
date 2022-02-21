using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using NUnit.Framework;

namespace Mongo.SignalR.Backplane.Tests;

/// <summary>
/// Base test class for lifetime manager implementations that support server scale-out.
/// </summary>
/// <typeparam name="TBackplane">An in-memory implementation of the backplane that <see cref="HubLifetimeManager{THub}"/>s communicate with.</typeparam>
[TestFixture]
[NonParallelizable]
public abstract class ScaleoutHubLifetimeManagerTests<TBackplane> : HubLifetimeManagerTestsBase<Hub>
{
    /// <summary>
    /// Method to create an implementation of an in-memory backplane for use in tests.
    /// </summary>
    /// <returns>The backplane implementation.</returns>
    public abstract TBackplane CreateBackplane();

    /// <summary>
    /// Method to create an implementation of <see cref="HubLifetimeManager{THub}"/> that uses the backplane from <see cref="CreateBackplane"/>.
    /// </summary>
    /// <param name="backplane">The backplane implementation for use in the <see cref="HubLifetimeManager{THub}"/>.</param>
    /// <returns></returns>
    public abstract MongoHubLifetimeManager<Hub> CreateNewHubLifetimeManager(TBackplane backplane);

    private async Task AssertMessageAsync(TestClient client)
    {
        var message = await client.ReadAsync().DefaultTimeout();
        Assert.IsInstanceOf<InvocationMessage>(message);
        var invocationMessage = (InvocationMessage) message;
        Assert.AreEqual("Hello", invocationMessage.Target);
        Assert.AreEqual(invocationMessage.Arguments.Length, 1);
        Assert.AreEqual("World", (string)invocationMessage.Arguments[0]!);
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Test, Category("unit")]
    public async Task InvokeAllAsyncWithMultipleServersWritesToAllConnectionsOutput()
    {
        var backplane = CreateBackplane();
        using var client1 = new TestClient();
        using var client2 = new TestClient();
        var manager1 = CreateNewHubLifetimeManager(backplane);
        var manager2 = CreateNewHubLifetimeManager(backplane);

        var connection1 = HubConnectionContextUtils.Create(client1.Connection);
        var connection2 = HubConnectionContextUtils.Create(client2.Connection);

        await manager1.OnConnectedAsync(connection1).DefaultTimeout();
        await manager2.OnConnectedAsync(connection2).DefaultTimeout();

        await manager1.SendAllAsync("Hello", new object[] {"World"}).DefaultTimeout();

        await AssertMessageAsync(client1);
        await AssertMessageAsync(client2);
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Test, Category("unit")]
    public async Task InvokeAllAsyncWithMultipleServersDoesNotWriteToDisconnectedConnectionsOutput()
    {
        var backplane = CreateBackplane();
        var manager1 = CreateNewHubLifetimeManager(backplane);
        var manager2 = CreateNewHubLifetimeManager(backplane);

        using var client1 = new TestClient();
        using var client2 = new TestClient();
        var connection1 = HubConnectionContextUtils.Create(client1.Connection);
        var connection2 = HubConnectionContextUtils.Create(client2.Connection);

        await manager1.OnConnectedAsync(connection1).DefaultTimeout();
        await manager2.OnConnectedAsync(connection2).DefaultTimeout();

        await manager2.OnDisconnectedAsync(connection2).DefaultTimeout();

        await manager2.SendAllAsync("Hello", new object[] { "World" }).DefaultTimeout();

        await AssertMessageAsync(client1);

        Assert.Null(client2.TryRead());
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Test, Category("unit")]
    public async Task InvokeConnectionAsyncOnServerWithoutConnectionWritesOutputToConnection()
    {
        var backplane = CreateBackplane();

        var manager1 = CreateNewHubLifetimeManager(backplane);
        var manager2 = CreateNewHubLifetimeManager(backplane);

        using var client = new TestClient();
        var connection = HubConnectionContextUtils.Create(client.Connection);

        await manager1.OnConnectedAsync(connection).DefaultTimeout();

        await manager2.SendConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" }).DefaultTimeout();

        await AssertMessageAsync(client);
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Test, Category("unit")]
    public async Task InvokeGroupAsyncOnServerWithoutConnectionWritesOutputToGroupConnection()
    {
        var backplane = CreateBackplane();

        var manager1 = CreateNewHubLifetimeManager(backplane);
        var manager2 = CreateNewHubLifetimeManager(backplane);

        using var client = new TestClient();
        var connection = HubConnectionContextUtils.Create(client.Connection);

        await manager1.OnConnectedAsync(connection).DefaultTimeout();

        await manager1.AddToGroupAsync(connection.ConnectionId, "name").DefaultTimeout();

        await manager2.SendGroupAsync("name", "Hello", new object[] { "World" }).DefaultTimeout();

        await AssertMessageAsync(client);
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Test, Category("unit")]
    public async Task DisconnectConnectionRemovesConnectionFromGroup()
    {
        var backplane = CreateBackplane();
        var manager = CreateNewHubLifetimeManager(backplane);

        using var client = new TestClient();
        var connection = HubConnectionContextUtils.Create(client.Connection);

        await manager.OnConnectedAsync(connection).DefaultTimeout();

        await manager.AddToGroupAsync(connection.ConnectionId, "name").DefaultTimeout();

        await manager.OnDisconnectedAsync(connection).DefaultTimeout();
        
        await manager.SendGroupAsync("name", "Hello", new object[] { "World" }).DefaultTimeout();

        Assert.Null(client.TryRead());
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Test, Category("unit")]
    public async Task RemoveGroupFromLocalConnectionNotInGroupDoesNothing()
    {
        var backplane = CreateBackplane();
        var manager = CreateNewHubLifetimeManager(backplane);

        using var client = new TestClient();
        var connection = HubConnectionContextUtils.Create(client.Connection);

        await manager.OnConnectedAsync(connection).DefaultTimeout();

        await manager.RemoveFromGroupAsync(connection.ConnectionId, "name").DefaultTimeout();
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Test, Category("unit")]
    public async Task RemoveGroupFromConnectionOnDifferentServerNotInGroupDoesNothing()
    {
        var backplane = CreateBackplane();
        var manager1 = CreateNewHubLifetimeManager(backplane);
        var manager2 = CreateNewHubLifetimeManager(backplane);

        using var client = new TestClient();
        var connection = HubConnectionContextUtils.Create(client.Connection);

        await manager1.OnConnectedAsync(connection).DefaultTimeout();

        await manager2.RemoveFromGroupAsync(connection.ConnectionId, "name").DefaultTimeout();
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Test, Category("unit")]
    public async Task AddGroupAsyncForConnectionOnDifferentServerWorks()
    {
        var backplane = CreateBackplane();
        var manager1 = CreateNewHubLifetimeManager(backplane);
        var manager2 = CreateNewHubLifetimeManager(backplane);

        using var client = new TestClient();
        var connection = HubConnectionContextUtils.Create(client.Connection);

        await manager1.OnConnectedAsync(connection).DefaultTimeout();

        await manager2.AddToGroupAsync(connection.ConnectionId, "name").DefaultTimeout();

        await manager2.SendGroupAsync("name", "Hello", new object[] { "World" }).DefaultTimeout();

        await AssertMessageAsync(client);
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Test, Category("unit")]
    public async Task AddGroupAsyncForLocalConnectionAlreadyInGroupDoesNothing()
    {
        var backplane = CreateBackplane();
        var manager = CreateNewHubLifetimeManager(backplane);

        using var client = new TestClient();
        var connection = HubConnectionContextUtils.Create(client.Connection);

        await manager.OnConnectedAsync(connection).DefaultTimeout();

        await manager.AddToGroupAsync(connection.ConnectionId, "name").DefaultTimeout();
        await manager.AddToGroupAsync(connection.ConnectionId, "name").DefaultTimeout();

        await manager.SendGroupAsync("name", "Hello", new object[] { "World" }).DefaultTimeout();

        await AssertMessageAsync(client);
        Assert.Null(client.TryRead());
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Test, Category("unit")]
    public async Task AddGroupAsyncForConnectionOnDifferentServerAlreadyInGroupDoesNothing()
    {
        var backplane = CreateBackplane();
        var manager1 = CreateNewHubLifetimeManager(backplane);
        var manager2 = CreateNewHubLifetimeManager(backplane);

        using var client = new TestClient();
        var connection = HubConnectionContextUtils.Create(client.Connection);

        await manager1.OnConnectedAsync(connection).DefaultTimeout();

        await manager1.AddToGroupAsync(connection.ConnectionId, "name").DefaultTimeout();
        await manager2.AddToGroupAsync(connection.ConnectionId, "name").DefaultTimeout();

        await manager2.SendGroupAsync("name", "Hello", new object[] { "World" }).DefaultTimeout();

        await AssertMessageAsync(client);
        Assert.Null(client.TryRead());
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Test, Category("unit")]
    public async Task RemoveGroupAsyncForConnectionOnDifferentServerWorks()
    {
        var backplane = CreateBackplane();
        var manager1 = CreateNewHubLifetimeManager(backplane);
        var manager2 = CreateNewHubLifetimeManager(backplane);

        using var client = new TestClient();
        var connection = HubConnectionContextUtils.Create(client.Connection);

        await manager1.OnConnectedAsync(connection).DefaultTimeout();

        await manager1.AddToGroupAsync(connection.ConnectionId, "name").DefaultTimeout();

        await manager2.SendGroupAsync("name", "Hello", new object[] { "World" }).DefaultTimeout();

        await AssertMessageAsync(client);

        await manager2.RemoveFromGroupAsync(connection.ConnectionId, "name").DefaultTimeout();

        await manager2.SendGroupAsync("name", "Hello", new object[] { "World" }).DefaultTimeout();

        Assert.Null(client.TryRead());
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Test, Category("unit")]
    public async Task InvokeConnectionAsyncForLocalConnectionDoesNotPublishToBackplane()
    {
        var backplane = CreateBackplane();
        var manager1 = CreateNewHubLifetimeManager(backplane);
        var manager2 = CreateNewHubLifetimeManager(backplane);

        using var client = new TestClient();
        var connection = HubConnectionContextUtils.Create(client.Connection);

        // Add connection to both "servers" to see if connection receives message twice
        await manager1.OnConnectedAsync(connection).DefaultTimeout();
        await manager2.OnConnectedAsync(connection).DefaultTimeout();

        await manager1.SendConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" }).DefaultTimeout();

        await AssertMessageAsync(client);
        Assert.Null(client.TryRead());
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Test, Category("unit")]
    public async Task WritingToRemoteConnectionThatFailsDoesNotThrow()
    {
        var backplane = CreateBackplane();
        var manager1 = CreateNewHubLifetimeManager(backplane);
        var manager2 = CreateNewHubLifetimeManager(backplane);

        using var client = new TestClient();
        // Force an exception when writing to connection
        var connectionMock = HubConnectionContextUtils.CreateMock(client.Connection);

        await manager2.OnConnectedAsync(connectionMock).DefaultTimeout();

        // This doesn't throw because there is no connection.ConnectionId on this server so it has to publish to the backplane.
        // And once that happens there is no way to know if the invocation was successful or not.
        await manager1.SendConnectionAsync(connectionMock.ConnectionId, "Hello", new object[] { "World" }).DefaultTimeout();
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Test, Category("unit")]
    public async Task WritingToGroupWithOneConnectionFailingSecondConnectionStillReceivesMessage()
    {
        var backplane = CreateBackplane();
        var manager = CreateNewHubLifetimeManager(backplane);

        using var client1 = new TestClient();
        using var client2 = new TestClient();
        // Force an exception when writing to connection
        var connectionMock = HubConnectionContextUtils.CreateMock(client1.Connection);

        var connection1 = connectionMock;
        var connection2 = HubConnectionContextUtils.Create(client2.Connection);

        await manager.OnConnectedAsync(connection1).DefaultTimeout();
        await manager.AddToGroupAsync(connection1.ConnectionId, "group");
        await manager.OnConnectedAsync(connection2).DefaultTimeout();
        await manager.AddToGroupAsync(connection2.ConnectionId, "group");

        await manager.SendGroupAsync("group", "Hello", new object[] { "World" }).DefaultTimeout();
        // connection1 will throw when receiving a group message, we are making sure other connections
        // are not affected by another connection throwing
        await AssertMessageAsync(client2);

        // Repeat to check that group can still be sent to
        await manager.SendGroupAsync("group", "Hello", new object[] { "World" }).DefaultTimeout();
        await AssertMessageAsync(client2);
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Test, Category("unit")]
    public async Task InvokeUserSendsToAllConnectionsForUser()
    {
        var backplane = CreateBackplane();
        var manager = CreateNewHubLifetimeManager(backplane);

        using var client1 = new TestClient();
        using var client2 = new TestClient();
        using var client3 = new TestClient();
        var connection1 = HubConnectionContextUtils.Create(client1.Connection, userIdentifier: "userA");
        var connection2 = HubConnectionContextUtils.Create(client2.Connection, userIdentifier: "userA");
        var connection3 = HubConnectionContextUtils.Create(client3.Connection, userIdentifier: "userB");

        await manager.OnConnectedAsync(connection1).DefaultTimeout();
        await manager.OnConnectedAsync(connection2).DefaultTimeout();
        await manager.OnConnectedAsync(connection3).DefaultTimeout();

        await manager.SendUserAsync("userA", "Hello", new object[] { "World" }).DefaultTimeout();
        await AssertMessageAsync(client1);
        await AssertMessageAsync(client2);
    }

    /// <summary>
    /// Specification test for SignalR HubLifetimeManager.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous completion of the test.</returns>
    [Test, Category("unit")]
    public async Task StillSubscribedToUserAfterOneOfMultipleConnectionsAssociatedWithUserDisconnects()
    {
        var backplane = CreateBackplane();
        var manager = CreateNewHubLifetimeManager(backplane);

        using var client1 = new TestClient();
        using var client2 = new TestClient();
        using var client3 = new TestClient();
        var connection1 = HubConnectionContextUtils.Create(client1.Connection, userIdentifier: "userA");
        var connection2 = HubConnectionContextUtils.Create(client2.Connection, userIdentifier: "userA");
        var connection3 = HubConnectionContextUtils.Create(client3.Connection, userIdentifier: "userB");

        await manager.OnConnectedAsync(connection1).DefaultTimeout();
        await manager.OnConnectedAsync(connection2).DefaultTimeout();
        await manager.OnConnectedAsync(connection3).DefaultTimeout();

        await manager.SendUserAsync("userA", "Hello", new object[] { "World" }).DefaultTimeout();
        await AssertMessageAsync(client1);
        await AssertMessageAsync(client2);

        // Disconnect one connection for the user
        await manager.OnDisconnectedAsync(connection1).DefaultTimeout();
        await manager.SendUserAsync("userA", "Hello", new object[] { "World" }).DefaultTimeout();
        await AssertMessageAsync(client2);
    }
}
