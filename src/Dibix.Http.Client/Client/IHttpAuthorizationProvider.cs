namespace Dibix.Http.Client
{
    public interface IHttpAuthorizationProvider
    {
        string GetValue(string headerName);
    }
}