﻿using Microsoft.AspNetCore.SignalR;

namespace MongoSignalR.Backplane.Sample;

public class NameUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User.Identity?.Name;
    }
}