using System;
using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SourceConfiguration
    {
        #region Properties
        public Type Parser { get; set; }
        public Type Formatter { get; set; }
        #endregion

        #region Public Methods
        public IEnumerable<SqlStatementInfo> CollectStatements()
        {
            if (this.Parser == null)
                throw new InvalidOperationException("No parser was configured");

            if (this.Formatter == null)
                throw new InvalidOperationException("No formatter was configured");

            ISqlStatementParser parser = (ISqlStatementParser)Activator.CreateInstance(this.Parser);
            ISqlStatementFormatter formatter = (ISqlStatementFormatter)Activator.CreateInstance(this.Formatter);
            return this.CollectStatements(parser, formatter);
        }
        #endregion

        #region Abstract Methods
        protected abstract IEnumerable<SqlStatementInfo> CollectStatements(ISqlStatementParser parser, ISqlStatementFormatter formatter);
        #endregion
    }
}