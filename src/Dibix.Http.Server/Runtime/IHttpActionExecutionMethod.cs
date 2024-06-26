﻿using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Http.Server
{
    public interface IHttpActionExecutionMethod
    {
        MethodInfo Method { get; }
        string Source { get; }

        Task<object> Execute(IControllerActivator controllerActivator, IDictionary<string, object> arguments, CancellationToken cancellationToken);
    }
}