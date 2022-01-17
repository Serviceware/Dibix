using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.VisualStudio
{
    internal abstract class InputSourceConfiguration
    {
        #region Properties
        public Type Parser { get; set; }
        public Type Formatter { get; set; }
        #endregion

        #region Public Methods
        public void Collect(CodeGenerationModel model, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            if (this.Parser == null)
                throw new InvalidOperationException("No parser was configured");

            if (this.Formatter == null)
                throw new InvalidOperationException("No formatter was configured");

            ISqlStatementParser parser = (ISqlStatementParser)Activator.CreateInstance(this.Parser);
            ISqlStatementFormatter formatter = (ISqlStatementFormatter)Activator.CreateInstance(this.Formatter);
            formatter.StripWhiteSpace = model.CommandTextFormatting == CommandTextFormatting.StripWhiteSpace;
            foreach (SqlStatementDefinition info in this.CollectStatements(parser, formatter, typeResolver, schemaRegistry, logger).Where(x => x != null)) 
                model.Schemas.Add(info);
        }
        #endregion

        #region Protected Methods
        protected abstract IEnumerable<SqlStatementDefinition> CollectStatements(ISqlStatementParser parser, ISqlStatementFormatter formatter, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, ILogger logger);
        #endregion
    }
}