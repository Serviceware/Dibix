using Xunit;

namespace Dibix.Sdk.Tests.CodeAnalysis
{
    public class SqlCodeAnalysisRuleTests : SqlCodeAnalysisRuleTestsBase
    {
        [Fact]
        public void CasingSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void SchemaSqlCodeAnalysisRule() => base.Execute();

        //[Fact]
        public void NoReturnSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void UnicodeConstantSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void UnicodeDataTypeSqlCodeAnalysisRule() => base.Execute();

        //[Fact]
        public void RedundantAliasSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void PrimitiveDataTypeIdentifierSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void ImplicitAliasSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void DataTypeLengthSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void MultiTableAliasSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void ConsistentlyQuotedIdentifierSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void HeapTableSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void CursorSqlCodeAnalysisRule() => base.Execute();
    }
}
