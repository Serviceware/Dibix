using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TextTemplating;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SqlAccessorGenerator : ISqlAccessorGenerator
    {
        #region Fields
        private readonly IExecutionEnvironment _environment;
        private readonly SqlAccessorGeneratorConfiguration _configuration;
        #endregion

        #region Constructor
        public SqlAccessorGenerator(IExecutionEnvironment environment)
        {
            this._environment = environment;
            this._configuration = new SqlAccessorGeneratorConfiguration();
        }
        #endregion

        #region ISqlAccessorGenerator Members
        public static ISqlAccessorGenerator Create(IExecutionEnvironment environment)
        {
            SqlAccessorGenerator generator = new SqlAccessorGenerator(environment);
            return generator;
        }

        public static ISqlAccessorGenerator FromVisualStudio(ITextTemplatingEngineHost host, IServiceProvider serviceProvider)
        {
            IExecutionEnvironment environment = new VisualStudioExecutionEnvironment(host, serviceProvider);
            SqlAccessorGenerator generator = new SqlAccessorGenerator(environment);
            return generator;
        }

        public ISqlAccessorGenerator AddSource(string projectName) { return this.AddSource(projectName, null); }
        public ISqlAccessorGenerator AddSource(Action<ISourceSelectionExpression> configuration) { return this.AddSource(null, configuration); }
        public ISqlAccessorGenerator AddSource(string projectName, Action<ISourceSelectionExpression> configuration)
        {
            if (String.IsNullOrEmpty(projectName))
                throw new ArgumentNullException(projectName);

            //if (!String.IsNullOrEmpty(projectName))
                this._environment.VerifyProject(projectName);

            SourceSelectionExpression expression = new SourceSelectionExpression(this._environment, projectName);
            configuration?.Invoke(expression);

            expression.Build();
            this._configuration.Sources.Add(expression);
            return this;
        }

        public ISqlAccessorGenerator SelectOutputWriter<TWriter>() where TWriter : IWriter, new() { return this.SelectOutputWriter<TWriter>(null); }
        public ISqlAccessorGenerator SelectOutputWriter<TWriter>(Action<IOutputConfigurationExpression> configuration) where TWriter : IWriter, new()
        {
            TWriter writer = new TWriter();
            OutputConfigurationExpression expression = new OutputConfigurationExpression(this._environment, writer);
            configuration?.Invoke(expression);

            expression.Build();
            this._configuration.Writer = writer;
            return this;
        }

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

            IList<SqlStatementInfo> statements = TraverseSources(environment, configuration.Sources).ToArray();
            string output = configuration.Writer.Write(environment.GetProjectName(), statements);
            if (environment.ReportErrors())
                output = "Please fix the errors first";

            return output;
        }

        private static IEnumerable<SqlStatementInfo> TraverseSources(IExecutionEnvironment environment, IEnumerable<ISourceSelection> sources)
        {
            foreach (ISourceSelection source in sources)
            {
                ISqlStatementParser parser = source.Parser ?? new NoOpParser();

                if (parser.Formatter == null)
                    parser.Formatter = new TakeSourceSqlStatementFormatter();

                foreach (string filePath in source.Files)
                {
                    yield return ParseStatement(environment, filePath, parser);
                }
            }
        }

        private static SqlStatementInfo ParseStatement(IExecutionEnvironment environment, string filePath, ISqlStatementParser parser)
        {
            using (Stream stream = File.OpenRead(filePath))
            {
                SqlStatementInfo statement = new SqlStatementInfo
                {
                    SourcePath = filePath,
                    Name = Path.GetFileNameWithoutExtension(filePath)
                };
                parser.Read(environment, stream, statement);
                return statement;
            }
        }
        #endregion
    }
}