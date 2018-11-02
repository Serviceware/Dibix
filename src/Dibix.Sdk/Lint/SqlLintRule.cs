using System;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk
{
    public abstract class SqlLintRule : TSqlFragmentVisitor
    {
        private IExecutionEnvironment _environment;
        private string _sourceFilePath;

        public abstract int Id { get; }
        public abstract string ErrorMessage { get; }

        internal void Execute(IExecutionEnvironment environment, TSqlFragment fragment, string sourceFilePath)
        {
            this._environment = environment;
            this._sourceFilePath = sourceFilePath;

            fragment.Visit(this.Visit);
            fragment.Accept(this);
        }

        protected void Fail(TSqlParserToken token, params object[] args)
        {
            string errorCode = String.Format(@"SQLLINT#{0:d3}", this.Id);
            string errorText = String.Format("[{0}] {1}", errorCode, String.Format(this.ErrorMessage, args));
            this._environment.RegisterError(this._sourceFilePath, token.Line, token.Column, errorCode, errorText);
        }

        protected virtual void Visit(TSqlParserToken token) { }
    }
}