using System;
using Microsoft.Extensions.Logging;

namespace Dibix.Http.Server
{
    public interface IHttpHostExtensionScope
    {
        IDatabaseAccessorFactory DatabaseAccessorFactory { get; }

        ILogger CreateLogger(Type loggerType);
    }
}