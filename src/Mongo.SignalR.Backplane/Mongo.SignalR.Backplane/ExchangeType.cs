namespace Mongo.SignalR.Backplane;

public enum ExchangeType
{
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