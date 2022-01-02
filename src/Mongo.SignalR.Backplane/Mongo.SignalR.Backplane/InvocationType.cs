namespace Mongo.SignalR.Backplane;

public enum InvocationType
{
    Init,
    SendAll,
    SendAllExcept,
    SendConnection,
    SendGroup,
    SendGroups,
    SendGroupExcept,
    SendUser,
    SendUsers,
    AddToGroup,
    RemoveFromGroup
}