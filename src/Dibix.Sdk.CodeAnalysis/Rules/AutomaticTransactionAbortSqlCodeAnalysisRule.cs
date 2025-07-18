using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 43)]
    public sealed class AutomaticTransactionAbortSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "SET XACT_ABORT ON when using multiple write statements";

        public AutomaticTransactionAbortSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context) { }

        public override void Visit(TSqlBatch node)
        {
            BatchVisitor visitor = new BatchVisitor();
            node.Accept(visitor);

            if (!visitor.Valid)
                Fail(node);
        }

        private sealed class BatchVisitor : TSqlFragmentVisitor
        {
            private int _modificationStatementCount;
            private bool _foundSetXactAbort;

            public bool Valid => _modificationStatementCount < 2 || _foundSetXactAbort;

            public override void ExplicitVisit(PredicateSetStatement node)
            {
                if (node.Options == SetOptions.XactAbort && node.IsOn)
                    _foundSetXactAbort = true;

                base.ExplicitVisit(node);
            }

            public override void Visit(DataModificationStatement node)
            {
                _modificationStatementCount++;
                base.Visit(node);
            }
        }
    }
}