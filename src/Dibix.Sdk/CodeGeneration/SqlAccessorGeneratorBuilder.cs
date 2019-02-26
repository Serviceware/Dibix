using System;
using Microsoft.VisualStudio.TextTemplating;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SqlAccessorGeneratorBuilder : ISqlAccessorGeneratorBuilder
    {
        #region Fields
        private readonly IExecutionEnvironment _environment;
        private readonly ISqlAccessorGeneratorConfigurationFactory _configurationFactory;
        private readonly SqlAccessorGeneratorConfiguration _configuration;
        #endregion

        #region Constructor
        private SqlAccessorGeneratorBuilder(IExecutionEnvironment environment, ISqlAccessorGeneratorConfigurationFactory configurationFactory)
        {
            this._environment = environment;
            this._configurationFactory = configurationFactory;
            this._configuration = this._configurationFactory.CreateConfiguration(environment);
        }
        #endregion

        #region Factory Members
        public static ISqlAccessorGeneratorBuilder Create(IExecutionEnvironment environment) => CreateFromEnvironment(environment);

        public static ISqlAccessorGeneratorBuilder CreateUsingVisualStudio(ITextTemplatingEngineHost host, IServiceProvider serviceProvider) => CreateUsingVisualStudioCore(host, serviceProvider);

        public static string GenerateFromJson(IExecutionEnvironment environment, string json)
        {
            SqlAccessorGeneratorBuilder builder = CreateFromEnvironment(environment);
            return GenerateFromJson(builder, json);
        }

        public static string GenerateFromJsonUsingVisualStudio(ITextTemplatingEngineHost host, IServiceProvider serviceProvider, string json)
        {
            SqlAccessorGeneratorBuilder builder = CreateUsingVisualStudioCore(host, serviceProvider);
            return GenerateFromJson(builder, json);
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

            PhysicalSourceConfiguration sourceConfiguration = this._configurationFactory.CreatePhysicalSourceConfiguration(this._environment, projectName);
            PhysicalSourceConfigurationExpression expression = new PhysicalSourceConfigurationExpression(sourceConfiguration);
            configuration?.Invoke(expression);
            this._configuration.Input.Sources.Add(sourceConfiguration);
            return this;
        }

        public ISqlAccessorGeneratorBuilder AddDacPac(string packagePath, Action<IDacPacSelectionExpression> configuration)
        {
            Guard.IsNotNull(configuration, nameof(configuration));
            DacPacSourceConfiguration sourceConfiguration = this._configurationFactory.CreateDacPacSourceConfiguration(this._environment, packagePath);
            DacPacSourceConfigurationExpression expression = new DacPacSourceConfigurationExpression(sourceConfiguration);
            configuration(expression);
            this._configuration.Input.Sources.Add(sourceConfiguration);
            return this;
        }

        public ISqlAccessorGeneratorBuilder SelectOutputWriter<TWriter>() where TWriter : IWriter { return this.SelectOutputWriter<TWriter>(null); }
        public ISqlAccessorGeneratorBuilder SelectOutputWriter<TWriter>(Action<IOutputConfigurationExpression> configuration) where TWriter : IWriter
        {
            OutputConfigurationExpression expression = new OutputConfigurationExpression(this._configuration.Output);
            configuration?.Invoke(expression);

            this._configuration.Output.Writer = typeof(TWriter);
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
            ISqlAccessorGeneratorConfigurationFactory configurationFactory = new SqlAccessorGeneratorConfigurationFactory();
            SqlAccessorGeneratorBuilder builder = new SqlAccessorGeneratorBuilder(environment, configurationFactory);
            return builder;
        }

        private static SqlAccessorGeneratorBuilder CreateUsingVisualStudioCore(ITextTemplatingEngineHost host, IServiceProvider serviceProvider)
        {
            IExecutionEnvironment environment = new VisualStudioExecutionEnvironment(host, serviceProvider);
            SqlAccessorGeneratorBuilder builder = CreateFromEnvironment(environment);
            return builder;
        }

        private static string GenerateFromJson(SqlAccessorGeneratorBuilder builder, string json)
        {
            builder._configuration.ApplyFromJson(json, builder._environment);
            string output = builder.Generate();
            return output;
        }
        #endregion
    }
}