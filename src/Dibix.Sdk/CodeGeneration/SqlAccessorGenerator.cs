using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class SqlAccessorGenerator : ICodeGenerator
    {
        #region Fields
        private readonly SqlAccessorGeneratorConfiguration _configuration;
        private readonly IExecutionEnvironment _environment;
        #endregion

        #region Constructor
        public SqlAccessorGenerator(SqlAccessorGeneratorConfiguration configuration, IExecutionEnvironment environment)
        {
            this._configuration = configuration;
            this._environment = environment;
        }
        #endregion

        #region ICodeGenerator Members
        public string Generate()
        {
            return Generate(this._environment, this._configuration);
        }
        #endregion

        #region Private Methods
        private static string Generate(IExecutionEnvironment environment, SqlAccessorGeneratorConfiguration configuration)
        {
            const string errorContent = "Please fix the errors first";
            string output;
            if (environment.ReportErrors())
            {
                output = errorContent;
                return output;
            }

            if (!configuration.Input.Sources.Any())
                throw new InvalidOperationException("No files were selected to scan");

            if (configuration.Output.Writer == null)
                throw new InvalidOperationException("No output writer was selected");

            IWriter writer = (IWriter)Activator.CreateInstance(configuration.Output.Writer);
            IList<SqlStatementInfo> statements = configuration.Input.Sources.SelectMany(x => x.CollectStatements()).ToArray();
            output = writer.Write(environment.GetProjectName(), configuration.Output.Namespace, configuration.Output.ClassName, configuration.Output.Formatting, statements);
            if (environment.ReportErrors())
                output = errorContent;

            return output;
        }
        #endregion
    }
}