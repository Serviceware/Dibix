using System;
using Microsoft.VisualStudio.TextTemplating;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SqlAccessorGeneratorBuilder : ISqlAccessorGeneratorBuilder
    {
        #region Fields
        private readonly IExecutionEnvironment _environment;
        private readonly SqlAccessorGeneratorConfiguration _configuration;
        #endregion

        #region Constructor
        private SqlAccessorGeneratorBuilder(IExecutionEnvironment environment)
        {
            this._environment = environment;
            this._configuration = new SqlAccessorGeneratorConfiguration();
        }
        #endregion

        #region Factory Members
        public static ISqlAccessorGeneratorBuilder Create(IExecutionEnvironment environment) => CreateFromEnvironment(environment);

        public static ISqlAccessorGeneratorBuilder FromVisualStudio(ITextTemplatingEngineHost host, IServiceProvider serviceProvider) => CreateFromVisualStudio(host, serviceProvider);

        public static string GenerateFromJson(ITextTemplatingEngineHost host, IServiceProvider serviceProvider, string json)
        {
            SqlAccessorGeneratorBuilder builder = CreateFromVisualStudio(host, serviceProvider);
            builder._configuration.ApplyFromJson(json);
            string output = builder.Generate();
            return output;
        }
        #endregion

        #region ISqlAccessorGeneratorBuilder Members
        public ISqlAccessorGeneratorBuilder AddSource(string projectName) { return this.AddSource(projectName, null); }
        public ISqlAccessorGeneratorBuilder AddSource(Action<IPhysicalSourceSelectionExpression> configuration) { return this.AddSource(null, configuration); }
        public ISqlAccessorGeneratorBuilder AddSource(string projectName, Action<IPhysicalSourceSelectionExpression> configuration)
        {
            Guard.IsNotNullOrEmpty(projectName, nameof(projectName));

            //if (!String.IsNullOrEmpty(projectName))
            this._environment.VerifyProject(projectName);

            PhysicalSourceSelectionExpression expression = new PhysicalSourceSelectionExpression(this._environment, projectName);
            configuration?.Invoke(expression);
            this._configuration.Sources.Add(expression);
            return this;
        }

        public ISqlAccessorGeneratorBuilder AddDacPac(string packagePath, Action<IDacPacSelectionExpression> configuration)
        {
            Guard.IsNotNull(configuration, nameof(configuration));
            DacPacSelectionExpression expression = new DacPacSelectionExpression(this._environment, packagePath);
            configuration(expression);
            this._configuration.Sources.Add(expression);
            return this;
        }

        public ISqlAccessorGeneratorBuilder SelectOutputWriter<TWriter>() where TWriter : IWriter, new() { return this.SelectOutputWriter<TWriter>(null); }
        public ISqlAccessorGeneratorBuilder SelectOutputWriter<TWriter>(Action<IOutputConfigurationExpression> configuration) where TWriter : IWriter, new()
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
            ICodeGenerator generator = new SqlAccessorGenerator(this._configuration, this._environment);
            string output = generator.Generate();
            return output;
        }
        #endregion

        #region Private Methods
        private static SqlAccessorGeneratorBuilder CreateFromEnvironment(IExecutionEnvironment environment)
        {
            SqlAccessorGeneratorBuilder builder = new SqlAccessorGeneratorBuilder(environment);
            return builder;
        }

        private static SqlAccessorGeneratorBuilder CreateFromVisualStudio(ITextTemplatingEngineHost host, IServiceProvider serviceProvider)
        {
            IExecutionEnvironment environment = new VisualStudioExecutionEnvironment(host, serviceProvider);
            SqlAccessorGeneratorBuilder builder = CreateFromEnvironment(environment);
            return builder;
        }
        #endregion
    }
}