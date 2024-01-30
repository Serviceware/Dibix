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
            AssertEqual(@".Lambda #Lambda1<Dibix.Http.Server.HttpActionExecutorResolver+ExecuteHttpAction>(System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments)
{
    .Block(
        System.Int32 $x,
        System.Threading.Tasks.Task`1[System.Object] $result) {
        $result = .Block() {
            .Call Dibix.Http.Server.Tests.HttpActionExecutorTest.CompileAndExecute_Void_AndOutParam_Target(
                .Call Dibix.Http.Server.HttpActionExecutorResolver.CollectParameter(
                    $arguments,
                    ""databaseAccessorFactory""),
                $x);
            .Call System.Threading.Tasks.Task.FromResult(null)
        };
        $arguments.Item[""x""] = (System.Object)$x;
        $result
    }
}", method.Source);

            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            IDictionary<string, object> arguments = new Dictionary<string, object> { ["databaseAccessorFactory"] = databaseAccessorFactory.Object };
            object result = await method.Execute(arguments, default).ConfigureAwait(false);

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
            AssertEqual(@".Lambda #Lambda1<Dibix.Http.Server.HttpActionExecutorResolver+ExecuteHttpAction>(System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments)
{
    .Call System.Threading.Tasks.Task.FromResult((System.Object).Call Dibix.Http.Server.Tests.HttpActionExecutorTest.CompileAndExecute_Result_Target(.Call Dibix.Http.Server.HttpActionExecutorResolver.CollectParameter(
                $arguments,
                ""databaseAccessorFactory"")))
}", method.Source);

            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            IDictionary<string, object> arguments = new Dictionary<string, object> { ["databaseAccessorFactory"] = databaseAccessorFactory.Object };
            object result = await method.Execute(arguments, default).ConfigureAwait(false);

            Assert.AreEqual(3, result);
            Assert.AreEqual(1, arguments.Count);
            Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
        }
        private static int CompileAndExecute_Result_Target(IDatabaseAccessorFactory databaseAccessorFactory) => 3;

        [TestMethod]
        public async Task CompileAndExecute_Task()
        {
            IHttpActionExecutionMethod method = Compile();
            AssertEqual(@".Lambda #Lambda1<Dibix.Http.Server.HttpActionExecutorResolver+ExecuteHttpAction>(System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments)
{
    .Call Dibix.Http.Server.HttpActionExecutorResolver.Convert(.Call Dibix.Http.Server.Tests.HttpActionExecutorTest.CompileAndExecute_Task_Target(.Call Dibix.Http.Server.HttpActionExecutorResolver.CollectParameter(
                $arguments,
                ""databaseAccessorFactory"")))
}", method.Source);

            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            IDictionary<string, object> arguments = new Dictionary<string, object> { ["databaseAccessorFactory"] = databaseAccessorFactory.Object };
            object result = await method.Execute(arguments, default).ConfigureAwait(false);

            Assert.IsNull(result);
            Assert.AreEqual(1, arguments.Count);
            Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
        }
        private static Task CompileAndExecute_Task_Target(IDatabaseAccessorFactory databaseAccessorFactory) => Task.CompletedTask;

        [TestMethod]
        public async Task CompileAndExecute_TaskResult()
        {
            IHttpActionExecutionMethod method = Compile();
            AssertEqual(@".Lambda #Lambda1<Dibix.Http.Server.HttpActionExecutorResolver+ExecuteHttpAction>(System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments)
{
    .Call Dibix.Http.Server.HttpActionExecutorResolver.Convert(.Call Dibix.Http.Server.Tests.HttpActionExecutorTest.CompileAndExecute_TaskResult_Target(.Call Dibix.Http.Server.HttpActionExecutorResolver.CollectParameter(
                $arguments,
                ""databaseAccessorFactory"")))
}", method.Source);
            
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            IDictionary<string, object> arguments = new Dictionary<string, object> { ["databaseAccessorFactory"] = databaseAccessorFactory.Object };
            object result = await method.Execute(arguments, default).ConfigureAwait(false);

            Assert.AreEqual(4, result);
            Assert.AreEqual(1, arguments.Count);
            Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
        }
        private static Task<int> CompileAndExecute_TaskResult_Target(IDatabaseAccessorFactory databaseAccessorFactory) => Task.FromResult(4);
    }
}