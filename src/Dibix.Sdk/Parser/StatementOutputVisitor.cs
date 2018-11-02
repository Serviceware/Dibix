using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk
{
    internal sealed class StatementOutputVisitor : StatementOutputVisitorBase
    {
        private readonly IExecutionEnvironment _environment;
        private readonly string _sourcePath;

        public IList<OutputSelectResult> Results { get; private set; }

        public StatementOutputVisitor(IExecutionEnvironment environment, string sourcePath)
        {
            this._environment = environment;
            this._sourcePath = sourcePath;
            this.Results = new Collection<OutputSelectResult>();
        }

        public override void ExplicitVisit(IfStatement node)
        {
            IfStatementOutputVisitor visitor = new IfStatementOutputVisitor(this._environment, this._sourcePath);
            visitor.Accept(node);
            this.Results.AddRange(visitor.Results);
        }

        protected override void OnOutputFound(OutputSelectResult result)
        {
            this.Results.Add(result);
        }
    }
}