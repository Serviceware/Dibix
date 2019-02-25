using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{

    public sealed class SqlAccessorGenerator : ICodeGenerator
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

        #region Generator
        public static string Generate(IExecutionEnvironment environment, SqlAccessorGeneratorConfiguration configuration)
        {
            if (!configuration.Sources.Any())
                throw new InvalidOperationException("No files were selected to scan");

            if (configuration.Writer == null)
                throw new InvalidOperationException("No output writer was selected");

            IList<SqlStatementInfo> statements = configuration.Sources.SelectMany(x => x.CollectStatements()).ToArray();
            string output = configuration.Writer.Write(environment.GetProjectName(), statements);
            if (environment.ReportErrors())
                output = "Please fix the errors first";

            return output;
        }
        #endregion
    }
}