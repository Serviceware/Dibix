using Xunit;

namespace Dibix.Sdk.Tests.CodeAnalysis
{
    public class SqlCodeAnalysisRuleTests : SqlCodeAnalysisRuleTestsBase
    {
        [Fact]
        public void KeywordCasingSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void SchemaSpecificationSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void NoReturnSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void UnicodeConstantSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void UnicodeDataTypeSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void RedundantAliasSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void PrimitiveDataTypeIdentifierSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void ImplicitAliasSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void UnspecifiedDataTypeLengthSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void UnaliasedTableJoinSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void ConsistentlyQuotedIdentifierSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void MissingPrimaryKeySqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void NoCursorSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void UnnamedConstraintSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void InsertWithoutColumnSpecificationSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void ObsoleteDataTypeSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void NamingConventionSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void UnfilteredDataModificationSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void TruncateTableSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void LanguageDependentConstantSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void TemporaryTableSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void TopWithoutOrderBySqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void PrimaryKeyDataTypeSqlCodeAnalysisRule() => base.Execute();
    }
}
