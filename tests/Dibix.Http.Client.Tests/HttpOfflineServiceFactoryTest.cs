using Dibix.Testing.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Http.Client.Tests
{
    [TestClass]
    public class HttpOfflineServiceFactoryTest
    {
        //[TestMethod]
        public void Test()
        {
            //SomeHttpService service = HttpOfflineServiceFactory.For<SomeHttpService, SomeHttpServiceBuilder>(x => x.Build());
            // TODO
        }

        private sealed class SomeHttpServiceBuilder : IHttpTestServiceBuilder<SomeHttpService>
        {
            public SomeHttpService Build() => new SomeHttpService();
        }

        private sealed class SomeHttpService
        {
        }
    }
}