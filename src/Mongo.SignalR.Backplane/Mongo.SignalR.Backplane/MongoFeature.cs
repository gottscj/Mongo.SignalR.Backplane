using System;
using System.Collections.Generic;

namespace Mongo.SignalR.Backplane;

public class MongoFeature
{
    public HashSet<string> Groups { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}