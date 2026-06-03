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
        var options = new HubConnectionContextOptions { KeepAliveInterval = TimeSpan.FromSeconds(15) };
        return new HubConnectionContext(connection, options, NullLoggerFactory.Instance)
        {
            Protocol = protocol ?? new JsonHubProtocol(),
            UserIdentifier = userIdentifier,
        };
    }

    public static MockHubConnectionContext CreateMock(ConnectionContext connection)
    {
        var options = new HubConnectionContextOptions
        {
            KeepAliveInterval = TimeSpan.FromSeconds(15),
            ClientTimeoutInterval = TimeSpan.FromSeconds(15)
        };
        return new MockHubConnectionContext(connection, options, NullLoggerFactory.Instance);
    }

    public class MockHubConnectionContext : HubConnectionContext
    {
        public MockHubConnectionContext(ConnectionContext connectionContext, HubConnectionContextOptions options, ILoggerFactory loggerFactory)
            : base(connectionContext, options, loggerFactory)
        {
        }

        public override ValueTask WriteAsync(HubMessage message, CancellationToken cancellationToken = default)
        {
            throw new Exception();
        }
    }
}
