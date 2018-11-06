namespace Dibix.Sdk.Tests
{
    public sealed class LintTests : LintTestBase
    {
        [Fact]
        public void SqlCasingLintRule() => base.RunLintTest(1);

        [Fact]
        public void SqlSchemaLintRule() => base.RunLintTest(2);

        [Fact]
        public void SqlNoReturnLintRule() => base.RunLintTest(3);

        [Fact]
        public void SqlUnicodeLintRule() => base.RunLintTest(4);
    }
}
