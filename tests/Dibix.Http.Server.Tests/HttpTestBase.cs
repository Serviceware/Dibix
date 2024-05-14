using System;
using Dibix.Testing;

namespace Dibix.Http.Server.Tests
{
    public abstract class HttpTestBase : TestBase
    {
        protected void AssertGeneratedText(string actualText)
        {
            const string extension = "txt";
            string expectedKey = $"{TestContext.TestName}.{extension}";
            string expectedText = GetEmbeddedResourceContent(expectedKey);
            AssertEqual(expectedText, actualText, extension);
        }

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