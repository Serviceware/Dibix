using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Dibix.Http.Server;
using Dibix.Http.Server.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Http.Host.Tests
{
    internal sealed class TestHttpApiDescriptor : HttpApiDescriptor
    {
        public TestHttpApiDescriptor()
        {
            Metadata.ProductName = "Dibix";
            Metadata.AreaName = "Tests";
        }

        public override void Configure(IHttpApiDiscoveryContext context)
        {
            foreach (IGrouping<Type, (Type Controller, MethodInfo Endpoint, MethodInfo? Authorization)> controller in CollectEndpoints().GroupBy(x => x.Controller))
            {
                RegisterController(controller.Key.Name, x =>
                {
                    foreach ((_, MethodInfo endpoint, MethodInfo? authorization) in controller)
                    {
                        IHttpActionTarget target = new MethodInfoHttpActionTarget(endpoint);
                        x.AddAction(target, y =>
                        {
                            y.Method = HttpApiMethod.Get;
                            y.RegisterDelegate((HttpContext httpContext, IHttpActionDelegator actionDelegator, CancellationToken cancellationToken) => actionDelegator.Delegate(httpContext, new Dictionary<string, object>(), cancellationToken));

                            ActionNameAttribute? actionNameAttribute = endpoint.GetCustomAttribute<ActionNameAttribute>();
                            if (actionNameAttribute != null)
                                y.ChildRoute = actionNameAttribute.Name;

                            if (authorization != null)
                                y.AddAuthorizationBehavior(new MethodInfoHttpActionTarget(authorization), _ => { });
                        });
                    }
                });
            }
        }

        private static IEnumerable<(Type Controller, MethodInfo Endpoint, MethodInfo? Authorization)> CollectEndpoints()
        {
            foreach (Type type in typeof(TestHttpApiDescriptor).Assembly.GetTypes())
            {
                if (!type.IsDefined(typeof(TestClassAttribute)))
                    continue;

                foreach (MethodInfo method in type.GetMethods())
                {
                    if (!method.IsDefined(typeof(TestMethodAttribute)))
                        continue;

                    MethodInfo? endpointMethod = type.GetMethod($"{method.Name}_Endpoint", BindingFlags.NonPublic | BindingFlags.Static);
                    if (endpointMethod == null)
                        continue;

                    MethodInfo? authorizationMethod = type.GetMethod($"{method.Name}_Authorization", BindingFlags.NonPublic | BindingFlags.Static);

                    yield return (Controller: type, Endpoint: endpointMethod, Authorization: authorizationMethod);
                }
            }
        }

        private sealed class MethodInfoHttpActionTarget : IHttpActionTarget
        {
            private readonly MethodInfo _method;

            public bool IsExternal => false;

            public MethodInfoHttpActionTarget(MethodInfo action) => _method = action;

            public MethodInfo Build() => _method;
        }
    }
}