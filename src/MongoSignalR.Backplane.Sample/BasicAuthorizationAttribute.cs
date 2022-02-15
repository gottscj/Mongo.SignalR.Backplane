using Microsoft.AspNetCore.Authorization;

namespace MongoSignalR.Backplane.Sample;

public class BasicAuthorizationAttribute : AuthorizeAttribute
{
    public BasicAuthorizationAttribute()
    {
        Policy = "BasicAuthentication";
    }
}