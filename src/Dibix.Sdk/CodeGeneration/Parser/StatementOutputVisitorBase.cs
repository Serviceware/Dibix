﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class StatementOutputVisitorBase : TSqlFragmentVisitor
    {
        #region Properties
        public string Statement { get; private set; }
        protected IList<OutputSelectResult> Outputs { get; }
        #endregion

        #region Constructor
        protected StatementOutputVisitorBase()
        {
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
            OutputColumnResult[] columns = selectElements.Select(VisitSelectElement).Where(x => x != null).ToArray();
            if (!columns.Any())
                return;

            OutputSelectResult result = new OutputSelectResult(index, line, column);
            result.Columns.AddRange(columns);

            this.Outputs.Add(result);
            this.OnOutputFound(result);
        }

        private static OutputColumnResult VisitSelectElement(SelectElement selectElement)
        {
            SelectScalarExpression scalar = selectElement as SelectScalarExpression;
            if (scalar == null)
                return null;

            if (scalar.ColumnName == null)
            {
                if (scalar.Expression is ColumnReferenceExpression columnReference)
                {
                    Identifier identifier = columnReference.MultiPartIdentifier.Identifiers.Last();
                    return OutputColumnResult.Success(identifier.Value, identifier.StartLine, identifier.StartColumn);
                }

                return OutputColumnResult.Fail(scalar.Dump(), scalar.StartLine, scalar.StartColumn);
            }

            return OutputColumnResult.Success(scalar.ColumnName.Value, scalar.ColumnName.Identifier.StartLine, scalar.ColumnName.Identifier.StartColumn);
        }
        #endregion
    }
}