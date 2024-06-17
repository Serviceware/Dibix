using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Sdk.Tests.CodeAnalysis
{
    public sealed partial class SqlCodeAnalysisRuleTests
    {
        [TestMethod]
        public void PostDeployScript() => ExecuteScript("PostDeploy");
    }
}