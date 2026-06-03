#nullable enable
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Mongo.SignalR.Backplane.Tests;

internal class TestHubProtocolResolver : IHubProtocolResolver
{
    private readonly IReadOnlyList<IHubProtocol> _protocols;

    public TestHubProtocolResolver(IReadOnlyList<IHubProtocol> protocols)
    {
        _protocols = protocols;
    }

    public IReadOnlyList<IHubProtocol> AllProtocols => _protocols;

    public IHubProtocol? GetProtocol(string protocolName, IReadOnlyList<string>? supportedProtocols)
    {
        return _protocols.FirstOrDefault(p =>
            p.Name.Equals(protocolName, StringComparison.OrdinalIgnoreCase) &&
            (supportedProtocols == null || supportedProtocols.Contains(p.Name)));
    }
}
