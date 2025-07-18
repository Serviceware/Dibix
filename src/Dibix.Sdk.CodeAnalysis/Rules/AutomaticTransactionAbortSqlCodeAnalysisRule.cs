using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 43, SupportsScriptArtifacts = false)]
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
            private bool _withinTransaction;

            public bool Valid => _modificationStatementCount < 2 || _foundSetXactAbort;

            public override void ExplicitVisit(PredicateSetStatement node)
            {
                if (node.Options == SetOptions.XactAbort && node.IsOn)
                    _foundSetXactAbort = true;

                base.ExplicitVisit(node);
            }

            public override void ExplicitVisit(BeginTransactionStatement node)
            {
                _withinTransaction = true;
                base.ExplicitVisit(node);
            }

            public override void ExplicitVisit(CommitTransactionStatement node)
            {
                _withinTransaction = false;
                base.ExplicitVisit(node);
            }

            public override void ExplicitVisit(RollbackTransactionStatement node)
            {
                _withinTransaction = false;
                base.ExplicitVisit(node);
            }

            public override void Visit(DataModificationSpecification node)
            {
                if (!_withinTransaction && node.Target is not VariableTableReference)
                    _modificationStatementCount++;

                base.Visit(node);
            }
        }
    }
}