using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mongo.SignalR.Backplane.Tests;
#if TESTUTILS
public
#else
internal
#endif
    static class HubConnectionContextUtils
{
    public static HubConnectionContext Create(ConnectionContext connection, IHubProtocol protocol = null, string userIdentifier = null)
    {
        return new HubConnectionContext(connection, TimeSpan.FromSeconds(15), NullLoggerFactory.Instance)
        {
            Protocol = protocol ?? new JsonHubProtocol(),
            UserIdentifier = userIdentifier,
        };
    }

    public static MockHubConnectionContext CreateMock(ConnectionContext connection)
    {
        var mock = new MockHubConnectionContext(connection, TimeSpan.FromSeconds(15), NullLoggerFactory.Instance,
            TimeSpan.FromSeconds(15));
        return mock;
    }

    public class MockHubConnectionContext : HubConnectionContext
    {
        public MockHubConnectionContext(ConnectionContext connectionContext, TimeSpan keepAliveInterval, ILoggerFactory loggerFactory, TimeSpan clientTimeoutInterval)
            : base(connectionContext, keepAliveInterval, loggerFactory, clientTimeoutInterval)
        {
        }

        public override ValueTask WriteAsync(HubMessage message, CancellationToken cancellationToken = default)
        {
            throw new Exception();
        }
    }
}
