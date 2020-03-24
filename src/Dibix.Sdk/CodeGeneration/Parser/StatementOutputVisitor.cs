using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class StatementOutputVisitor : StatementOutputVisitorBase
    {
        private readonly string _sourcePath;

        public IList<OutputSelectResult> Results { get; }

        public StatementOutputVisitor(string sourcePath, TSqlElementLocator elementLocator, ILogger logger) : base(sourcePath, elementLocator, logger)
        {
            this._sourcePath = sourcePath;
            this.Results = new Collection<OutputSelectResult>();
        }

        public override void ExplicitVisit(IfStatement node)
        {
            IfStatementOutputVisitor visitor = new IfStatementOutputVisitor(this._sourcePath, base.ElementLocator, base.Logger);
            visitor.Accept(node);
            this.Results.AddRange(visitor.Results);
        }

        protected override void OnOutputFound(OutputSelectResult result)
        {
            this.Results.Add(result);
        }
    }
}