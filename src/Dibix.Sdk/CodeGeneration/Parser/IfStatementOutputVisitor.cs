using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal class IfOutputResolutionContext
    {
        public bool IsResolved { get; private set; }
        public IList<OutputSelectResult> Outputs { get; }

        public IfOutputResolutionContext()
        {
            this.Outputs = new Collection<OutputSelectResult>();
        }

        public void Resolve(IEnumerable<OutputSelectResult> outputs)
        {
            this.IsResolved = true;
            this.Outputs.AddRange(outputs);
        }
    }

    internal sealed class IfStatementOutputVisitor : StatementOutputVisitorBase
    {
        #region Fields
        private readonly IfOutputResolutionContext _context;
        private readonly string _sourcePath;
        private bool _containsIf;
        #endregion

        #region Properties
        public IList<OutputSelectResult> Results => this._context.Outputs;
        #endregion

        #region Constructor
        public IfStatementOutputVisitor(string sourcePath, TSqlFragmentAnalyzer fragmentAnalyzer, ILogger logger) : this(sourcePath, fragmentAnalyzer, logger, new IfOutputResolutionContext()) { }
        public IfStatementOutputVisitor(string sourcePath, TSqlFragmentAnalyzer fragmentAnalyzer, ILogger logger, IfOutputResolutionContext context) : base(sourcePath, fragmentAnalyzer, logger)
        {
            this._sourcePath = sourcePath;
            this._context = context;
        }
        #endregion

        #region Overrides
        public override void Accept(TSqlFragment fragment)
        {
            this._containsIf = fragment.ContainsIf();
            base.Accept(fragment);
        }

        public override void ExplicitVisit(IfStatement node)
        {
            IfStatementOutputVisitor left = new IfStatementOutputVisitor(this._sourcePath, base.FragmentAnalyzer, base.Logger, this._context);
            left.Accept(node.ThenStatement);

            // This might be an IF block without ELSE
            // This is only allowed, if the IF block is not producing any outputs (i.E. RAISERROR)
            if (node.ElseStatement != null)
            {
                IfStatementOutputVisitor right = new IfStatementOutputVisitor(this._sourcePath, base.FragmentAnalyzer, base.Logger, this._context);
                right.Accept(node.ElseStatement);

                // Compare output statements between IF..THEN and ELSE
                IList<OutputSelectResult> leftResults = left._containsIf ? left.Results : left.Outputs;
                IList<OutputSelectResult> rightResults = right._containsIf ? right.Results : right.Outputs;
                if (leftResults.Count != rightResults.Count)
                {
                    base.Logger.LogError(null, "The number of output statements in IF THEN block does not match the number in ELSE block", this._sourcePath, node.StartLine, node.StartColumn);
                    return;
                }

                for (int i = 0; i < leftResults.Count; i++)
                {
                    OutputSelectResult leftResult = leftResults[i];
                    OutputSelectResult rightResult = rightResults[i];

                    if (leftResult.Columns.Count != rightResult.Columns.Count)
                    {
                        base.Logger.LogError(null, "The number of columns in output statement in IF THEN block does not match the number in ELSE block", this._sourcePath, leftResult.Line, leftResult.Column);
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
Column in ELSE: {rightColumn.ColumnName}", this._sourcePath, leftColumn.ColumnNameSource.StartLine, leftColumn.ColumnNameSource.StartColumn);
                            break;
                        }
                    }
                }

                // Use output statements of IF..THEN now that they should be validated as equal
                if (!this._context.IsResolved)
                    this._context.Resolve(left.Outputs);

                //this._visitedStatements.AddRange(right.Results.Select(x => x.Index));
            }
            else if (left.Outputs.Any())
                base.Logger.LogError(null, "IF statements that produce outputs but do not have an ELSE block are not supported, because the number of output results isn't guaranteed", this._sourcePath, node.StartLine, node.StartColumn);
        }
        #endregion
    }
}