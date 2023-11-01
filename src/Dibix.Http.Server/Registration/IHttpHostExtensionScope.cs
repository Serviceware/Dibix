namespace Dibix.Http.Server
{
    public interface IHttpHostExtensionScope
    {
        IDatabaseAccessorFactory DatabaseAccessorFactory { get; }

#if NET
        Microsoft.Extensions.Logging.ILogger CreateLogger(System.Type loggerType);
#endif
    }
}