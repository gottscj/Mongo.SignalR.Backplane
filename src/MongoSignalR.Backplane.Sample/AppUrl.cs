namespace MongoSignalR.Backplane.Sample;

public class AppUrl
{
    public AppUrl(string url)
    {
        if (url.EndsWith("/"))
        {
            Url = url.Remove(url.Length-1);
        }
        else
        {
            Url = url;
        }
    }

    public string Url { get; }

    public override string ToString()
    {
        return Url;
    }
}