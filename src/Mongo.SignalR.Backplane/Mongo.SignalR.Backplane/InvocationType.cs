﻿namespace Mongo.SignalR.Backplane;

public static class InvocationType
{
    public const string Init = "Init";
    public const string All = "All";
    public const string Connection = "Connection";
    public const string Group = "Group";
    public const string User = "User";
    public const string AddToGroup = "AddToGroup";
    public const string RemoveFromGroup = "RemoveFromGroup";
}