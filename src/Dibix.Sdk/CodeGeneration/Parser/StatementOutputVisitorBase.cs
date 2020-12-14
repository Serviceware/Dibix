using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class StatementOutputVisitorBase : TSqlFragmentVisitor
    {
        #region Fields
        private readonly string _sourcePath;
        #endregion

        #region Properties
        public string Statement { get; private set; }
        protected TSqlFragmentAnalyzer FragmentAnalyzer { get; }
        protected IList<OutputSelectResult> Outputs { get; }
        protected ILogger Logger { get; }
        #endregion

        #region Constructor
        protected StatementOutputVisitorBase(string sourcePath, TSqlFragmentAnalyzer fragmentAnalyzer, ILogger logger)
        {
            this._sourcePath = sourcePath;
            this.FragmentAnalyzer = fragmentAnalyzer;
            this.Logger = logger;
            this.Outputs = new Collection<OutputSelectResult>();
        }
        #endregion

        #region Overrides
        public virtual void Accept(TSqlFragment fragment)
        {
            this.Statement = fragment.Dump();
            fragment.Accept(this);
        }

        public override void Visit(SelectStatement node)
        {
            if (node.Into != null)
                return;

            QuerySpecification query = node.QueryExpression.FindQuerySpecification();
            if (query == null)
                return;

            this.Visit(query.FirstTokenIndex, query.StartLine, query.StartColumn, query.SelectElements);
        }

        public override void Visit(DataModificationSpecification node)
        {
            if (node.OutputClause == null)
                return;

            this.Visit(node.OutputClause.FirstTokenIndex, node.OutputClause.StartLine, node.OutputClause.StartColumn, node.OutputClause.SelectColumns);
        }
        #endregion

        #region Protected Methods
        protected virtual void OnOutputFound(OutputSelectResult result) { }
        #endregion

        #region Private Methods
        private void Visit(int index, int line, int column, IEnumerable<SelectElement> selectElements)
        {
            OutputColumnResult[] columns = selectElements.Select(this.VisitSelectElement).Where(x => x != null).ToArray();
            if (!columns.Any())
                return;

            OutputSelectResult result = new OutputSelectResult(index, line, column);
            result.Columns.AddRange(columns);

            this.Outputs.Add(result);
            this.OnOutputFound(result);
        }

        private OutputColumnResult VisitSelectElement(SelectElement selectElement)
        {
            if (selectElement is SelectStarExpression)
            {
                this.Logger.LogError(null, "Star expressions are not supported", this._sourcePath, selectElement.StartLine, selectElement.StartColumn);
                return null;
            }

            if (!(selectElement is SelectScalarExpression scalar))
                return null;

            this.FragmentAnalyzer.TryGetModelElement(selectElement, out TSqlObject modelElement);
            SqlDataType dataType = selectElement.GetDataType(modelElement);
            bool? isNullable = selectElement.IsNullable(modelElement);

            if (scalar.ColumnName == null)
            {
                if (scalar.Expression is ColumnReferenceExpression columnReference)
                {
                    if (columnReference.ColumnType != Microsoft.SqlServer.TransactSql.ScriptDom.ColumnType.Regular)
                    {
                        this.Logger.LogError(null, $"Cannot determine name for unaliased column: {columnReference.Dump()}", this._sourcePath, selectElement.StartLine, selectElement.StartColumn);
                        return null;
                    }

                    Identifier identifier = columnReference.GetName();
                    return new OutputColumnResult(identifier.Value, selectElement, scalar.Expression, dataType, isNullable);
                }

                return new OutputColumnResult(null, scalar, null, dataType, isNullable);
            }

            return new OutputColumnResult(scalar.ColumnName.Value, selectElement, scalar.ColumnName, dataType, isNullable);
        }
        #endregion
    }
}