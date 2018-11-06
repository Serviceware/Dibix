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
        public bool HasError { get; private set; }

        internal void Execute(IExecutionEnvironment environment, TSqlFragment fragment, string sourceFilePath)
        {
            this._environment = environment;
            this._sourceFilePath = sourceFilePath;

            fragment.Visit(this.Visit);
            fragment.Accept(this);
        }

        protected void Fail(TSqlFragment fragment, params object[] args) => this.Fail(fragment.ScriptTokenStream[fragment.FirstTokenIndex], args);
        protected void Fail(TSqlParserToken token, params object[] args)
        {
            string errorCode = $"SQLLINT#{this.Id:d3}";
            string errorText = $"[{errorCode}] {String.Format(this.ErrorMessage, args)}";
            this._environment.RegisterError(this._sourceFilePath, token.Line, token.Column, errorCode, errorText);
            this.HasError = true;
        }

        protected virtual void Visit(TSqlParserToken token) { }
    }
}