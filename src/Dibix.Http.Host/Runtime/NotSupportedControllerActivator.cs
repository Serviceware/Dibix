using System;
using Dibix.Http.Server;

namespace Dibix.Http.Host
{
    internal sealed class NotSupportedControllerActivator : IControllerActivator
    {
        TInstance IControllerActivator.CreateInstance<TInstance>() => throw new NotSupportedException("Reflection targets are not supported on this platform");
    }
}