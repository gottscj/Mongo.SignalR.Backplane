using System;
using System.ComponentModel.DataAnnotations;

namespace Mongo.SignalR.Backplane;

public class MongoOptions
{
    /// <summary>
    /// Database name, will be overriden of connectionString contains database name
    /// default = 'signalr-backplane'
    /// </summary>
    public string DatabaseName { get; set; } = "signalr-backplane";

    /// <summary>
    /// Collection name used for invocations
    /// default = 'invocations'
    /// </summary>
    public string CollectionName { get; set; } = "invocations";

    /// <summary>
    /// Max collection size in MB
    /// default = '64'
    /// </summary>
    public int MaxSize { get; set; } = 64;

    // Use the machine name for convenient diagnostics, but add a guid to make it unique.
    // Example: MyServerName_02db60e5fab243b890a847fa5c4dcb29
    public string? ServerName { get; set; } = $"{Environment.MachineName}_{Guid.NewGuid():N}";

    /// <summary>
    /// Time to add to backoff time if failure to tail collection.
    /// default = 5000 ms
    /// </summary>
    public int FailureBackoffTimeoutMs { get; set; } = 5000;
    
    /// <summary>
    /// Max timeout to wait before retrying to connect to db.
    /// default = 60000 ms
    /// </summary>
    public int MaxTimeoutMs { get; set; } = 60000;
}