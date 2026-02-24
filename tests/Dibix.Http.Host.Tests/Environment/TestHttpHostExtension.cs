using Dibix.Http.Server.AspNetCore;

namespace Dibix.Http.Host.Tests
{
    public sealed class TestHttpHostExtension : IHttpHostExtension
    {
        public void Register(IHttpHostExtensionConfigurationBuilder builder)
        {
            builder.RegisterClaimsTransformer<TestClaimsTransformer>();
        }
    }
}