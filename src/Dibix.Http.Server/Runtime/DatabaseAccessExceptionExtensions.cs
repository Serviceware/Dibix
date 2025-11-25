using System.Net;

namespace Dibix.Http.Server
{
    public static class DatabaseAccessExceptionExtensions
    {
        private const string IsClientErrorPropertyKey = "Dibix.Http.Host.IsClientError";
        private const string HttpStatusCodePropertyKey = "Dibix.Http.Host.HttpStatusCode";

        extension(DatabaseAccessException exception)
        {
            public bool IsClientError
            {
                get => GetLazy<bool>(exception, IsClientErrorPropertyKey);
                set => exception.Data[IsClientErrorPropertyKey] = value;
            }

            public HttpStatusCode HttpStatusCode
            {
                get => GetLazy<HttpStatusCode>(exception, HttpStatusCodePropertyKey);
                set => exception.Data[HttpStatusCodePropertyKey] = value;
            }
        }

        private static T GetLazy<T>(DatabaseAccessException exception, string key)
        {
            object value = exception.Data[key];
            if (value == null)
            {
                Initialize(exception);
                return GetLazy<T>(exception, key);
            }
            return (T)value;
        }

        private static void Initialize(DatabaseAccessException exception)
        {
            SqlHttpStatusCodeParser.Collect(exception);
        }
    }
}