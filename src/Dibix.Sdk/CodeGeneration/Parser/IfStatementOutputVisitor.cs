using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class IfStatementOutputVisitor : StatementOutputVisitorBase
    {
        public IfStatementOutputVisitor(string sourcePath, TSqlFragmentAnalyzer fragmentAnalyzer, ILogger logger) : base(sourcePath, fragmentAnalyzer, logger) { }

        public override void ExplicitVisit(IfStatement node)
        {
            IfStatementOutputVisitor left = new IfStatementOutputVisitor(base.SourcePath, base.FragmentAnalyzer, base.Logger);
            left.Accept(node.ThenStatement);

            // This might be an IF block without ELSE
            // This is only allowed, if the IF block is not producing any outputs (i.E. RAISERROR)
            if (node.ElseStatement != null)
            {
                IfStatementOutputVisitor right = new IfStatementOutputVisitor(base.SourcePath, base.FragmentAnalyzer, base.Logger);
                right.Accept(node.ElseStatement);

                // Compare output statements between IF..THEN and ELSE
                if (left.Outputs.Count != right.Outputs.Count)
                {
                    base.Logger.LogError(null, "The number of output statements in IF THEN block does not match the number in ELSE block", base.SourcePath, node.StartLine, node.StartColumn);
                    return;
                }

                for (int i = 0; i < left.Outputs.Count; i++)
                {
                    OutputSelectResult leftResult = left.Outputs[i];
                    OutputSelectResult rightResult = right.Outputs[i];

                    if (leftResult.Columns.Count != rightResult.Columns.Count)
                    {
                        base.Logger.LogError(null, "The number of columns in output statement in IF THEN block does not match the number in ELSE block", base.SourcePath, leftResult.Line, leftResult.Column);
                        break;
                    }

                    for (int j = 0; j < leftResult.Columns.Count; j++)
                    {
                        OutputColumnResult leftColumn = leftResult.Columns[j];
                        OutputColumnResult rightColumn = rightResult.Columns[j];

                        if (leftColumn.ColumnName != rightColumn.ColumnName)
                        {
                            base.Logger.LogError(null, $@"The column names in output statement in IF THEN block do not match those in ELSE block
Column in THEN: {leftColumn.ColumnName}
Column in ELSE: {rightColumn.ColumnName}", base.SourcePath, leftColumn.ColumnNameSource.StartLine, leftColumn.ColumnNameSource.StartColumn);
                            break;
                        }
                    }
                }

                // Use output statements of IF..THEN now that they should be validated as equal
                this.Outputs.AddRange(left.Outputs);
            }
            else if (left.Outputs.Any())
                base.Logger.LogError(null, "IF statements that produce outputs but do not have an ELSE block are not supported, because the number of output results isn't guaranteed", base.SourcePath, node.StartLine, node.StartColumn);
        }
    }
}