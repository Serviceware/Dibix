using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SourceSelection : ISourceSelection
    {
        #region Properties
        public ISqlStatementParser Parser { get; set; }
        public ISqlStatementFormatter Formatter { get; set; }
        #endregion

        #region ISourceSelection Members
        IEnumerable<SqlStatementInfo> ISourceSelection.CollectStatements()
        {
            if (this.Parser == null)
                this.Parser = new NoOpParser();

            if (this.Formatter == null)
                this.Formatter = new TakeSourceSqlStatementFormatter();

            return this.CollectStatements();
        }
        #endregion

        #region Abstract Methods
        protected abstract IEnumerable<SqlStatementInfo> CollectStatements();
        #endregion
    }
}