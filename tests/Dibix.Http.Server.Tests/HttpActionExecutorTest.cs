using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Dibix.Http.Server.Tests
{
    [TestClass]
    public partial class HttpActionExecutorTest
    {
        [TestMethod]
        public async Task CompileAndExecute_Void_AndOutParam()
        {
            IHttpActionExecutionMethod method = Compile();
            AssertGeneratedText(method.Source);

            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            IDictionary<string, object> arguments = new Dictionary<string, object> { ["databaseAccessorFactory"] = databaseAccessorFactory.Object };
            object result = await method.Execute(ControllerActivator.NotImplemented, arguments, default).ConfigureAwait(false);

            Assert.IsNull(result);
            Assert.AreEqual(2, arguments.Count);
            Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.AreEqual(2, arguments["x"]);
        }
        private static void CompileAndExecute_Void_AndOutParam_Target(IDatabaseAccessorFactory databaseAccessorFactory, out int x) => x = 2;

        [TestMethod]
        public async Task CompileAndExecute_Result()
        {
            IHttpActionExecutionMethod method = Compile();
            AssertGeneratedText(method.Source);

            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            IDictionary<string, object> arguments = new Dictionary<string, object> { ["databaseAccessorFactory"] = databaseAccessorFactory.Object };
            object result = await method.Execute(ControllerActivator.Instance(this), arguments, default).ConfigureAwait(false);

            Assert.AreEqual(3, result);
            Assert.AreEqual(1, arguments.Count);
            Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
        }
        private int CompileAndExecute_Result_Target(IDatabaseAccessorFactory databaseAccessorFactory) => 3;

        [TestMethod]
        public async Task CompileAndExecute_Task()
        {
            IHttpActionExecutionMethod method = Compile();
            AssertGeneratedText(method.Source);

            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            IDictionary<string, object> arguments = new Dictionary<string, object> { ["databaseAccessorFactory"] = databaseAccessorFactory.Object };
            object result = await method.Execute(ControllerActivator.NotImplemented, arguments, default).ConfigureAwait(false);

            Assert.IsNull(result);
            Assert.AreEqual(1, arguments.Count);
            Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
        }
        private static Task CompileAndExecute_Task_Target(IDatabaseAccessorFactory databaseAccessorFactory) => Task.CompletedTask;

        [TestMethod]
        public async Task CompileAndExecute_TaskResult()
        {
            IHttpActionExecutionMethod method = Compile();
            AssertGeneratedText(method.Source);
            
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            IDictionary<string, object> arguments = new Dictionary<string, object> { ["databaseAccessorFactory"] = databaseAccessorFactory.Object };
            object result = await method.Execute(ControllerActivator.NotImplemented, arguments, default).ConfigureAwait(false);

            Assert.AreEqual(4, result);
            Assert.AreEqual(1, arguments.Count);
            Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
        }
        private static Task<int> CompileAndExecute_TaskResult_Target(IDatabaseAccessorFactory databaseAccessorFactory) => Task.FromResult(4);
    }
}