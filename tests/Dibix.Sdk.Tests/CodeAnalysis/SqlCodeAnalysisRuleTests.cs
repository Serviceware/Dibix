using Xunit;

namespace Dibix.Sdk.Tests.CodeAnalysis
{
    public sealed partial class SqlCodeAnalysisRuleTests
    {
        [Fact]
        public void KeywordCasingSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void SchemaSpecificationSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void EmptyReturnSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void UnicodeConstantSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void UnicodeDataTypeSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void RedundantAliasSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void PrimitiveDataTypeIdentifierSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void ImplicitAliasSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void UnspecifiedDataTypeLengthSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void AliasSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void ConsistentlyQuotedIdentifierSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void MissingPrimaryKeySqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void NoCursorSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void UnnamedConstraintSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void InsertWithoutColumnSpecificationSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void UnsupportedDataTypeSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void NamingConventionSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void UnfilteredDataModificationSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void TruncateTableSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void LanguageDependentConstantSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void TemporaryTableSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void MisusedTopRowFilterSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void PrimaryKeyDataTypeSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void SurrogateKeySqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void ImplicitDefaultSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void LooseConstraintsSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void UniqueIndexSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void DateTimeSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void TableConstraintSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void SecurityAlgorithmSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void UnintentionalBooleanComparisonSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void IndexSizeSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void RedundantIndexSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void IsNullSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void AmbiguousCheckConstraintSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void PrimaryKeyUpdateSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void InlineFunctionSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void RedundantSymbolSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void ExplicitProcedureArgumentSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void UnsupportedEmbeddedSymbolReferenceSqlCodeAnalysisRule() => this.Execute();

        [Fact]
        public void InvalidFunctionUsageSqlCodeAnalysisRule() => this.Execute();
    }
}