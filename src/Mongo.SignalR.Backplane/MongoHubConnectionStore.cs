using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using Mongo.SignalR.Backplane.Invocations;

namespace Mongo.SignalR.Backplane;

public class MongoHubConnectionStore : IEnumerable<HubConnectionContext>
{
    private readonly ConcurrentDictionary<string, HubConnectionContext> _connections;
    private readonly ConcurrentDictionary<string, HubConnectionStore> _groups;
    private readonly ConcurrentDictionary<string, HubConnectionStore> _users;
    

    public MongoHubConnectionStore()
    {
        _connections = new ConcurrentDictionary<string, HubConnectionContext>();
        _groups = new ConcurrentDictionary<string, HubConnectionStore>();
        _users = new ConcurrentDictionary<string, HubConnectionStore>();
    }
    public bool AddToGroup(string connectionId, string groupName)
    {
        if (!_connections.TryGetValue(connectionId, out var connection))
        {
            return false;
        }

        lock (_groups)
        {
            var store = _groups.GetOrAdd(groupName, _ => new HubConnectionStore());
            store.Add(connection);
        }

        return true;
    }

    public void RemoveFromGroup(string connectionId, string groupName)
    {
        lock (_groups)
        {
            if (_groups.TryGetValue(groupName, out var connectionStore))
            {
                if (_connections.TryGetValue(connectionId, out var connection))
                {
                    connectionStore.Remove(connection);
                }
            }
        }
    }
    
    public void AddToGroup(MongoInvocation invocation)
    {
        var connectionId = invocation.ConnectionIds?.FirstOrDefault() ?? "";
        var groupName = invocation.GroupNames?.FirstOrDefault() ?? "";
        
        if (!string.IsNullOrEmpty(groupName))
        {
            AddToGroup(connectionId, groupName);
        }
    }
    public void RemoveFromGroup(MongoInvocation invocation)
    {
        var connectionId = invocation.ConnectionIds?.FirstOrDefault() ?? "";
        var groupName = invocation.GroupNames?.FirstOrDefault() ?? "";
        if (!string.IsNullOrEmpty(groupName))
        {
            RemoveFromGroup(connectionId, groupName);
        }
    }
    
    public IList<HubConnectionContext> GetUserConnections(string userId, HashSet<string>? exclude)
    {
        var userConnections = new List<HubConnectionContext>();
        if (string.IsNullOrEmpty(userId))
        {
            return userConnections;
        }

        lock (_users)
        {
            if (_users.TryGetValue(userId, out var connectionStore))
            {
                foreach (var connection in connectionStore)
                {
                    if (exclude?.Contains(connection.ConnectionId) != true)
                    {
                        userConnections.Add(connection);
                    }
                }
                
            }
        }
        return userConnections;
    }
    
    public IList<HubConnectionContext> GetGroupConnections(string groupName, HashSet<string>? exclude)
    {
        var groupConnections = new List<HubConnectionContext>();
        if (string.IsNullOrEmpty(groupName))
        {
            return groupConnections;
        }

        lock (_groups)
        {
            if (!_groups.TryGetValue(groupName, out var connections))
            {
                return groupConnections;
            }
            
            foreach (var connection in connections)
            {
                if (exclude?.Contains(connection.ConnectionId) != true)
                {
                    groupConnections.Add(connection);
                }
            }
        }

        return groupConnections;
    }

    public HubConnectionContext? this[string connectionId]
    {
        get { _connections.TryGetValue(connectionId, out var connection);
            return connection;
        }
    }


    public IEnumerator<HubConnectionContext> GetEnumerator()
    {
        return _connections.Select(c => c.Value).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(HubConnectionContext connection)
    {
        _connections.TryAdd(connection.ConnectionId, connection);
        if (!string.IsNullOrEmpty(connection.UserIdentifier))
        {
            lock (_users)
            {
                var userConnections = _users.GetOrAdd(connection.UserIdentifier, _ => new HubConnectionStore());
                userConnections.Add(connection);
            }
        }
    }

    public void Remove(HubConnectionContext connection)
    {
        _connections.TryRemove(connection.ConnectionId, out _);
        if (!string.IsNullOrEmpty(connection.UserIdentifier))
        {
            lock (_users)
            {
                if (_users.TryGetValue(connection.UserIdentifier, out var users))
                {
                    users.Remove(connection);
                }
            }
        }
        
        lock (_groups)
        {
            foreach (var store in _groups.Select(g => g.Value))
            {
                store.Remove(connection);
            }
        }
    }
}