using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class StatementOutputVisitor : StatementOutputVisitorBase
    {
        public StatementOutputVisitor(string sourcePath, TSqlFragmentAnalyzer fragmentAnalyzer, ILogger logger) : base(sourcePath, fragmentAnalyzer, logger) { }

        public override void ExplicitVisit(IfStatement node)
        {
            IfStatementOutputVisitor visitor = new IfStatementOutputVisitor(base.SourcePath, base.FragmentAnalyzer, base.Logger);
            visitor.Accept(node);
            base.Outputs.AddRange(visitor.Outputs);
        }
    }
}