using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoCodeGenerator : ICodeGenerator
    {
        #region Fields
        private readonly ICodeGenerationContext _context;
        #endregion

        #region Constructor
        public DaoCodeGenerator(ICodeGenerationContext context)
        {
            this._context = context;
        }
        #endregion

        #region ICodeGenerator Members
        public string Generate()
        {
            const string errorContent = "\"Please fix the errors first\"";
            string output;
            if (this._context.ErrorReporter.HasErrors)
            {
                output = errorContent;
                return output;
            }

            this.ApplyConfigurationDefaults();

            IWriter writer = (IWriter)Activator.CreateInstance(this._context.Configuration.Output.Writer);
            SourceArtifacts artifacts = new SourceArtifacts();
            foreach (InputSourceConfiguration input in this._context.Configuration.Input.Sources)
                input.Collect(artifacts, this._context.ContractResolver, this._context.ErrorReporter);

            this._context.CollectAdditionalArtifacts(artifacts);

            output = writer.Write(this._context.Configuration.Output, artifacts);
            if (this._context.ErrorReporter.HasErrors)
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

            if (this._context.Configuration.Output.RootNamespace == null)
                this._context.Configuration.Output.RootNamespace = this._context.RootNamespace;

            if (this._context.Configuration.Output.DefaultClassName == null)
                this._context.Configuration.Output.DefaultClassName = this._context.DefaultClassName;

            if (this._context.Configuration.Output.Formatting == default)
                this._context.Configuration.Output.Formatting = CommandTextFormatting.Singleline;
        }
        #endregion
    }
}