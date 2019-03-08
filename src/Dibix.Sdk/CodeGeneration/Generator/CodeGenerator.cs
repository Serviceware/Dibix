using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class CodeGenerator : ICodeGenerator
    {
        #region Fields
        private readonly ICodeGenerationContext _context;
        #endregion

        #region Constructor
        public CodeGenerator(ICodeGenerationContext context)
        {
            this._context = context;
        }
        #endregion

        #region ICodeGenerator Members
        public string Generate()
        {
            const string errorContent = "\"Please fix the errors first\"";
            string output;
            if (this._context.Configuration.IsInvalid)
            {
                output = errorContent;
                return output;
            }

            this.ApplyConfigurationDefaults();

            IWriter writer = (IWriter)Activator.CreateInstance(this._context.Configuration.Output.Writer);
            IList<SqlStatementInfo> statements = this._context.Configuration.Input.Sources.SelectMany(x => x.CollectStatements(this._context.TypeLoaderFacade, this._context.ErrorReporter)).ToArray();
            output = writer.Write(this._context.Configuration.Output.Namespace, this._context.Configuration.Output.ClassName, this._context.Configuration.Output.Formatting.Value, statements);
            if (this._context.ErrorReporter.ReportErrors())
                output = errorContent;

            return output;
        }

        private void ApplyConfigurationDefaults()
        {
            foreach (InputSourceConfiguration inputSource in this._context.Configuration.Input.Sources)
            {
                if (inputSource.Parser == null)
                    inputSource.Parser = typeof(SqlStoredProcedureParser);

                if (inputSource.Formatter == null)
                    inputSource.Formatter = typeof(TakeSourceSqlStatementFormatter);
            }

            if (this._context.Configuration.Output.Writer == null)
                this._context.Configuration.Output.Writer = typeof(DaoWriter);

            if (this._context.Configuration.Output.Namespace == null)
                this._context.Configuration.Output.Namespace = this._context.Namespace;

            if (this._context.Configuration.Output.ClassName == null)
                this._context.Configuration.Output.ClassName = this._context.ClassName;

            if (this._context.Configuration.Output.Formatting == null)
                this._context.Configuration.Output.Formatting = CommandTextFormatting.Singleline;
        }
        #endregion
    }
}