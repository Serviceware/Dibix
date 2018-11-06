using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk
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
        private readonly IExecutionEnvironment _environment;
        private readonly IfOutputResolutionContext _context;
        private readonly string _sourcePath;
        private bool _containsIf;
        #endregion

        #region Properties
        public IList<OutputSelectResult> Results => this._context.Outputs;
        #endregion

        #region Constructor
        public IfStatementOutputVisitor(IExecutionEnvironment environment, string sourcePath) : this(environment, sourcePath, new IfOutputResolutionContext()) { }
        public IfStatementOutputVisitor(IExecutionEnvironment environment, string sourcePath, IfOutputResolutionContext context)
        {
            this._environment = environment;
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
            IfStatementOutputVisitor left = new IfStatementOutputVisitor(this._environment, this._sourcePath, this._context);
            left.Accept(node.ThenStatement);

            // This might be an IF block without ELSE
            // This is only allowed, if the IF block is not producing any outputs (i.E. RAISERROR)
            if (node.ElseStatement != null)
            {
                IfStatementOutputVisitor right = new IfStatementOutputVisitor(this._environment, this._sourcePath, this._context);
                right.Accept(node.ElseStatement);

                // Compare output statements between IF..THEN and ELSE
                IList<OutputSelectResult> leftResults = left._containsIf ? left.Results : left.Outputs;
                IList<OutputSelectResult> rightResults = right._containsIf ? right.Results : right.Outputs;
                if (leftResults.Count != rightResults.Count)
                {
                    this._environment.RegisterError(this._sourcePath, node.StartLine, node.StartColumn, null, "The number of output statements in IF THEN block does not match the number in ELSE block");
                    return;
                }

                for (int i = 0; i < leftResults.Count; i++)
                {
                    OutputSelectResult leftResult = leftResults[i];
                    OutputSelectResult rightResult = rightResults[i];

                    if (leftResult.Columns.Count != rightResult.Columns.Count)
                    {
                        this._environment.RegisterError(this._sourcePath, leftResult.Line, leftResult.Column, null, "The number of columns in output statement in IF THEN block does not match the number in ELSE block");
                        break;
                    }

                    for (int j = 0; j < leftResult.Columns.Count; j++)
                    {
                        OutputColumnResult leftColumn = leftResult.Columns[j];
                        OutputColumnResult rightColumn = rightResult.Columns[j];

                        if (leftColumn.ColumnName != rightColumn.ColumnName)
                        {
                            this._environment.RegisterError(this._sourcePath, leftColumn.Line, leftColumn.Column, null, $@"The column names in output statement in IF THEN block do not match those in ELSE block
Column in THEN: {leftColumn.ColumnName}
Column in ELSE: {rightColumn.ColumnName}");
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
                this._environment.RegisterError(this._sourcePath, node.StartLine, node.StartColumn, null, "IF statements that produce outputs but do not have an ELSE block are not supported, because the number of output results isn't guaranteed");
        }
        #endregion
    }
}