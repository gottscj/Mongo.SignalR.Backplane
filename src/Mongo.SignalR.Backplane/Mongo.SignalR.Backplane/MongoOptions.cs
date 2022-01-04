using System;

namespace Mongo.SignalR.Backplane;

public class MongoOptions
{
    public string DatabaseName { get; set; } = "signalr.backplane";

    // Use the machine name for convenient diagnostics, but add a guid to make it unique.
    // Example: MyServerName_02db60e5fab243b890a847fa5c4dcb29
    public string? ServerName { get; set; } = $"{Environment.MachineName}_{Guid.NewGuid():N}";
}