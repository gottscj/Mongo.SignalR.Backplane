using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson.Serialization.Attributes;

namespace Mongo.SignalR.Backplane;

public class MongoInvocationMessage
{
    public static MongoInvocationMessage FromSerializedMessage(SerializedMessage message)
    {
        var isArray = MemoryMarshal.TryGetArray(message.Serialized, out var array);
        Debug.Assert(isArray);
        if (array.Array == null)
        {
            throw new InvalidDataException($"Array is null for protocol: '{message.ProtocolName}'");
        }
        return new MongoInvocationMessage(message.ProtocolName, array.Array);
    }

    public SerializedMessage ToSerializedMessage()
    {
        return new SerializedMessage(ProtocolName, new ReadOnlyMemory<byte>(Serialized));
    }
    [BsonConstructor]
    public MongoInvocationMessage(string protocolName, byte[] serialized)
    {
        ProtocolName = protocolName;
        Serialized = serialized;
    }
    public string ProtocolName { get; private set; }
    public byte[] Serialized { get; private set; }
}