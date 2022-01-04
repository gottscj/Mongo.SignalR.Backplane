// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;

namespace Mongo.SignalR.Backplane;

public class MongoSubscriptionManager
{
    private readonly ConcurrentDictionary<string, HubConnectionStore> _subscriptions = new(StringComparer.Ordinal);

    public List<HubConnectionContext> GetConnections(string id)
    {
        HubConnectionStore hubConnectionStore;
        lock (_subscriptions)
        {
            _subscriptions.TryGetValue(id, out hubConnectionStore);
        }

        var connections = new List<HubConnectionContext>();
        
        if (hubConnectionStore != null)
        {
            foreach (var connection in hubConnectionStore)
            {
                connections.Add(connection);
            }
        }

        return connections;
    }
    
    public void AddSubscription(string id, HubConnectionContext connection)
    {
        lock (_subscriptions)
        {
            var subscription = _subscriptions.GetOrAdd(id, _ => new HubConnectionStore());

            subscription.Add(connection);
        }
    }

    public void RemoveSubscriptionAsync(string id, HubConnectionContext connection)
    {
        lock (_subscriptions)
        {
            if (!_subscriptions.TryGetValue(id, out var subscription))
            {
                return;
            }

            subscription.Remove(connection);

            if (subscription.Count == 0)
            {
                _subscriptions.TryRemove(id, out _);
            }
        }
    }
}