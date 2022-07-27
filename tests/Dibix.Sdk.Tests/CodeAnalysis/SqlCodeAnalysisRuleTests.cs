using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Sdk.Tests.CodeAnalysis
{
    [TestClass]
    public sealed partial class SqlCodeAnalysisRuleTests
    {
        [TestMethod]
        public void KeywordCasingSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void SchemaSpecificationSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void EmptyReturnSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void UnicodeConstantSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void UnicodeDataTypeSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void RedundantAliasSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void PrimitiveDataTypeIdentifierSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void ImplicitAliasSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void UnspecifiedDataTypeLengthSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void AliasSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void ConsistentlyQuotedIdentifierSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void MissingPrimaryKeySqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void NoCursorSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void UnnamedConstraintSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void InsertWithoutColumnSpecificationSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void UnsupportedDataTypeSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void NamingConventionSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void UnfilteredDataModificationSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void TruncateTableSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void LanguageDependentConstantSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void TemporaryTableSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void MisusedTopRowFilterSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void PrimaryKeyDataTypeSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void SurrogateKeySqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void ImplicitDefaultSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void LooseConstraintsSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void UniqueIndexSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void DateTimeSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void TableConstraintSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void SecurityAlgorithmSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void UnintentionalBooleanComparisonSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void IndexSizeSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void RedundantIndexSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void IsNullSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void AmbiguousCheckConstraintSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void PrimaryKeyUpdateSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void InlineFunctionSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void RedundantSymbolSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void ExplicitProcedureArgumentSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void UnsupportedEmbeddedSymbolReferenceSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void InvalidFunctionUsageSqlCodeAnalysisRule() => this.Execute();

        [TestMethod]
        public void SetStatementSqlCodeAnalysisRule() => this.Execute();
    }
}