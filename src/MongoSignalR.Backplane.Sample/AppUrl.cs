namespace MongoSignalR.Backplane.Sample;

public class AppUrl
{
    public AppUrl(string url)
    {
        Url = new Uri(url);
    }

    public Uri Url { get; }

    public override string ToString()
    {
        return Url.ToString();
    }
}