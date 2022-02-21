using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Mongo.SignalR.Backplane.Invocations;

[BsonDiscriminator(InvocationType.Base)]
[BsonKnownTypes(
    typeof(InitMongoInvocation),
    typeof(SendAllMongoInvocation), 
    typeof(ConnectionMongoInvocation), 
    typeof(GroupMongoInvocation), 
    typeof(UserMongoInvocation), 
    typeof(AddToGroupMongoInvocation), 
    typeof(RemoveFromGroupMongoInvocation))]
public class MongoInvocation
{
    public static MongoInvocation SendAll(IEnumerable<SerializedMessage> messages) => 
        new SendAllMongoInvocation(messages);
    public static MongoInvocation SendAll(IEnumerable<SerializedMessage> messages, IEnumerable<string>? excludedConnectionIds) =>
        new SendAllMongoInvocation(messages, excludedConnectionIds);

    public static MongoInvocation Connection(IEnumerable<SerializedMessage> messages, string connectionId) =>
        new ConnectionMongoInvocation(messages, connectionId);
    public static MongoInvocation Connection(IEnumerable<SerializedMessage> messages, IEnumerable<string> connectionIds) =>
        new ConnectionMongoInvocation(messages, connectionIds);

    public static MongoInvocation SendGroup(IEnumerable<SerializedMessage> messages, string group) =>
        new GroupMongoInvocation(messages, groupNames: new[] {group});

    public static MongoInvocation SendGroup(IEnumerable<SerializedMessage> messages, string group, IEnumerable<string>? excludedConnectionIds) =>
        new GroupMongoInvocation(messages, 
            excludedConnectionIds: excludedConnectionIds,
            groupNames: new[] {group});

    public static MongoInvocation SendGroup(IEnumerable<SerializedMessage> messages, IEnumerable<string> groups) =>
        new GroupMongoInvocation(messages, groupNames: groups);
    
    public static MongoInvocation SendUsers(IEnumerable<SerializedMessage> messages, IEnumerable<string> users) =>
        new UserMongoInvocation(messages, users: users);
    
    public static MongoInvocation SendUser(IEnumerable<SerializedMessage> messages, string user) =>
        new UserMongoInvocation( messages, users: new[] {user});

    public static MongoInvocation AddToGroup(string connectionId, string group) =>
        new AddToGroupMongoInvocation(connectionId, group);
    
    public static MongoInvocation RemoveFromGroup(string connectionId, string group) =>
        new RemoveFromGroupMongoInvocation(connectionId, group);
    

    [BsonConstructor]
    public MongoInvocation(
        ObjectId id, 
        HashSet<string>? excludedConnectionIds,
        HashSet<string>? connectionIds,
        HashSet<string>? groupNames,
        HashSet<string>? users,
        List<MongoInvocationMessage> messages)
    {
        Id = id;
        ExcludedConnectionIds = excludedConnectionIds;
        ConnectionIds = connectionIds;
        GroupNames = groupNames;
        Users = users;
        Messages = messages;
    }

    protected MongoInvocation(
        IEnumerable<SerializedMessage> messages,
        IEnumerable<string>? excludedConnectionIds = null,
        IEnumerable<string>? connectionIds = null,
        IEnumerable<string>? groupNames = null,
        IEnumerable<string>? users = null)
    {
        Id = ObjectId.GenerateNewId();
        Messages = messages.Select(MongoInvocationMessage.FromSerializedMessage).ToList();
        if (excludedConnectionIds != null)
        {
            ExcludedConnectionIds = new HashSet<string>(excludedConnectionIds);
        }
        if (connectionIds != null)
        {
            ConnectionIds = new HashSet<string>(connectionIds);
        }
        if (groupNames != null)
        {
            GroupNames = new HashSet<string>(groupNames);
        }
        if (users != null)
        {
            Users = new HashSet<string>(users);
        }
        
    }
    public ObjectId Id { get; private set; }
    public HashSet<string>? ExcludedConnectionIds { get; private set; }
    public HashSet<string>? ConnectionIds { get; private set; }
    public HashSet<string>? GroupNames { get; private set; }
    public HashSet<string>? Users { get; private set; }
    public List<MongoInvocationMessage> Messages { get; set; }
    
    public string? ServerName { get; set; }

    protected SerializedHubMessage GetSerializedHubMessage()
    {
        var hubMessage =
            new SerializedHubMessage(Messages.Select(m => m.ToSerializedMessage()).ToList());
        return hubMessage;
    }
    public virtual Task Process(MongoHubConnectionStore connections)
    {
        return Task.CompletedTask;
    }
}