using Xunit;

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
        public void SqlUnicodeConstantLintRule() => base.RunLintTest(4);

        [Fact]
        public void SqlUnicodeTypeLintRule() => base.RunLintTest(5);

        [Fact]
        public void SqlRedundantAliasLintRule() => base.RunLintTest(6);
    }
}
