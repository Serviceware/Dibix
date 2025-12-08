using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dibix.Http.Server;
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
            foreach (IGrouping<Type, MethodInfo> controller in CollectEndpoints().GroupBy(x => x.DeclaringType))
            {
                RegisterController(controller.Key.Name, x =>
                {
                    foreach (MethodInfo action in controller)
                    {
                        IHttpActionTarget target = new MethodInfoHttpActionTarget(action);
                        x.AddAction(target, y =>
                        {
                            Type[] delegateTypeParameters = action.GetParameters()
                                                                  .Select(p => p.ParameterType)
                                                                  .Concat([action.ReturnType])
                                                                  .ToArray();
                            Type delegateType = Expression.GetDelegateType(delegateTypeParameters);
                            Delegate @delegate = action.CreateDelegate(delegateType);
                            y.Method = HttpApiMethod.Get;
                            y.RegisterDelegate(@delegate);

                            ActionNameAttribute actionNameAttribute = action.GetCustomAttribute<ActionNameAttribute>();
                            if (actionNameAttribute != null)
                                y.ChildRoute = actionNameAttribute.Name;
                        });
                    }
                });
            }
        }

        private static IEnumerable<MethodInfo> CollectEndpoints()
        {
            foreach (Type type in typeof(TestHttpApiDescriptor).Assembly.GetTypes())
            {
                if (!type.IsDefined(typeof(TestClassAttribute)))
                    continue;

                foreach (MethodInfo method in type.GetMethods())
                {
                    if (!method.IsDefined(typeof(TestMethodAttribute)))
                        continue;

                    MethodInfo endpointMethod = type.GetMethod($"{method.Name}_Endpoint", BindingFlags.NonPublic | BindingFlags.Static);
                    if (endpointMethod == null)
                        continue;

                    yield return endpointMethod;
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