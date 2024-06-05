using System;
using Dibix.Testing;

namespace Dibix.Http.Server.Tests
{
    public abstract class HttpTestBase : TestBase
    {
        protected void AssertGeneratedText(string actualText) => AssertEqual(actualText, extension: "txt");

        protected sealed class ControllerActivator : IControllerActivator
        {
            private readonly Func<Type, object> _handler;

            public static readonly IControllerActivator NotImplemented = new ControllerActivator(_ => throw new NotImplementedException());
            public static readonly IControllerActivator EmptyCtor = new ControllerActivator(Activator.CreateInstance);
            public static IControllerActivator Instance(object instance) => new ControllerActivator(_ => instance);

            private ControllerActivator(Func<Type, object> handler) => _handler = handler;

            public TInstance CreateInstance<TInstance>() => (TInstance)_handler(typeof(TInstance));
        }
    }
}