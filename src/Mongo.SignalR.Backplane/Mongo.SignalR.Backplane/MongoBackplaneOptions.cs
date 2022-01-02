namespace Mongo.SignalR.Backplane;

public class MongoBackplaneOptions
{
    public string DatabaseName { get; set; } = "signalr";
    public string CollectionName { get; set; } = "stack-exchange";

}