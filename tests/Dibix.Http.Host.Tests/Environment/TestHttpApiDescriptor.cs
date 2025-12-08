using System.Data;
using Dibix.Http.Server;

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
            RegisterController(nameof(ExceptionHandlingTests), x =>
            {
                IHttpActionTarget target = new LocalReflectionHttpActionTarget(context, typeof(TestHttpApiDescriptor), nameof(ExceptionHandlingTests.InvokeEndpoint_WithClientValidationError_ProducedByThrow_ReturnsProblemDetailsWithCodeAndMessage));
                x.AddAction(target, y =>
                {
                    y.Method = HttpApiMethod.Get;
                    y.RegisterDelegate(InvokeEndpoint_WithClientValidationError_ProducedByThrow_ReturnsProblemDetailsWithCodeAndMessage);
                });
            });
        }

        private static void InvokeEndpoint_WithClientValidationError_ProducedByThrow_ReturnsProblemDetailsWithCodeAndMessage(IDatabaseAccessorFactory databaseAccessorFactory)
        {
            using IDatabaseAccessor accessor = databaseAccessorFactory.Create();
            accessor.Execute("THROW 400001, N'Oops', 1", CommandType.Text, ParametersVisitor.Empty);
        }
    }
}