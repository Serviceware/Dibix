using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SourceSelection : ISourceSelection
    {
        #region Properties
        public ISqlStatementParser Parser { get; set; }
        #endregion

        #region ISourceSelection Members
        IEnumerable<SqlStatementInfo> ISourceSelection.CollectStatements()
        {
            if (this.Parser == null)
                this.Parser = new NoOpParser();

            if (this.Parser.Formatter == null)
                this.Parser.Formatter = new TakeSourceSqlStatementFormatter();

            return this.CollectStatements();
        }
        #endregion

        #region Abstract Methods
        protected abstract IEnumerable<SqlStatementInfo> CollectStatements();
        #endregion
    }
}