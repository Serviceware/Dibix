using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class SqlAccessorGeneratorBuilder : ISqlAccessorGeneratorBuilder
    {
        #region Fields
        private readonly IExecutionEnvironment _environment;
        private readonly ISqlAccessorGeneratorConfigurationFactory _configurationFactory;
        private readonly SqlAccessorGeneratorConfiguration _configuration;
        #endregion

        #region Constructor
        public SqlAccessorGeneratorBuilder(IExecutionEnvironment environment, ISqlAccessorGeneratorConfigurationFactory configurationFactory)
        {
            this._environment = environment;
            this._configurationFactory = configurationFactory;
            this._configuration = this._configurationFactory.CreateConfiguration(environment);
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
    }
}