using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class InputSourceConfiguration
    {
        #region Properties
        public Type Parser { get; set; }
        public Type Formatter { get; set; }
        #endregion

        #region Public Methods
        public void Collect(SourceArtifacts artifacts, IContractResolverFacade contractResolverFacade, IErrorReporter errorReporter)
        {
            if (this.Parser == null)
                throw new InvalidOperationException("No parser was configured");

            if (this.Formatter == null)
                throw new InvalidOperationException("No formatter was configured");

            ISqlStatementParser parser = (ISqlStatementParser)Activator.CreateInstance(this.Parser);
            ISqlStatementFormatter formatter = (ISqlStatementFormatter)Activator.CreateInstance(this.Formatter);
            artifacts.Statements.AddRange(this.CollectStatements(parser, formatter, contractResolverFacade, errorReporter).Where(x => x != null));
        }
        #endregion

        #region Protected Methods
        protected abstract IEnumerable<SqlStatementInfo> CollectStatements(ISqlStatementParser parser, ISqlStatementFormatter formatter, IContractResolverFacade contractResolverFacade, IErrorReporter errorReporter);
        #endregion
    }
}