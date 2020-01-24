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
        public void EmptyReturnSqlCodeAnalysisRule() => base.Execute();

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
        public void AliasSqlCodeAnalysisRule() => base.Execute();

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
        public void MisusedTopRowFilterSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void PrimaryKeyDataTypeSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void SurrogateKeySqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void ImplicitDefaultSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void LooseConstraintsSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void UniqueIndexSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void DateTimeSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void TableConstraintSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void SecurityAlgorithmSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void UnintentionalBooleanComparisonSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void IndexSizeSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void RedundantIndexSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void IsNullSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void AmbiguousCheckConstraintSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void PrimaryKeyUpdateSqlCodeAnalysisRule() => base.Execute();

        [Fact]
        public void InlineFunctionSqlCodeAnalysisRule() => base.Execute();
    }
}