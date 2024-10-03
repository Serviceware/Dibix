using System.Threading.Tasks;
using Dibix.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Dapper.Tests
{
    [TestClass]
    public class TestInitialize : TestBase<TestConfiguration>
    {
        [AssemblyInitialize]
        public static async Task AssemblyInitialize(TestContext context)
        {
            await AssemblyInitialize<TestInitialize>(context).ConfigureAwait(false);
        }

        protected override async Task OnAssemblyInitialize()
        {
            await ContainerServices.CreateAsync(Out, AddTestRunFile).ConfigureAwait(false);
        }

        [AssemblyCleanup]
        public static async Task AssemblyCleanup()
        {
            if (ContainerServices.IsInitialized)
                await ContainerServices.Instance.DisposeAsync().ConfigureAwait(false);
        }
    }
}