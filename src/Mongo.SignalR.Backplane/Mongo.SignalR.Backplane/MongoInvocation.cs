using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;
using MongoDB.Bson;

namespace Mongo.SignalR.Backplane;

public class MongoInvocation
{
    public static MongoInvocation SendAll(string methodName, object[] args) => 
        new MongoInvocation(InvocationType.SendAll, methodName, args);
    public static MongoInvocation SendAllExcept(string methodName, object[] args, IEnumerable<string> excludedConnectionIds) =>
        new MongoInvocation(InvocationType.SendAllExcept, methodName, args, excludedConnectionIds: excludedConnectionIds);
    public MongoInvocation(
        InvocationType type, 
        string methodName,
        object[] args,
        IEnumerable<string> excludedConnectionIds = null,
        string connectionId = null,
        string groupName = null)
    {
        Id = ObjectId.GenerateNewId();
        Type = type;
        MethodName = methodName;
        Args = args.Select(BsonValue.Create).ToList();
        ExcludedConnectionIds = excludedConnectionIds?.ToList();
        ConnectionId = connectionId;
        GroupName = groupName;
    }
    public ObjectId Id { get; private set; }
    public InvocationType Type { get; private set; }
    public List<string> ExcludedConnectionIds { get; private set; }
    public string ConnectionId { get; private set; }
    public string GroupName { get; private set; }
    public string MethodName { get; private set; }
    public List<BsonValue> Args { get; private set; }
}