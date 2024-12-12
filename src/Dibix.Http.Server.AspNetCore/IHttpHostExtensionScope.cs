using System;
using Microsoft.Extensions.Logging;

namespace Dibix.Http.Server.AspNetCore
{
    public interface IHttpHostExtensionScope
    {
        IDatabaseAccessorFactory DatabaseAccessorFactory { get; }

        ILogger CreateLogger(Type loggerType);
    }
}