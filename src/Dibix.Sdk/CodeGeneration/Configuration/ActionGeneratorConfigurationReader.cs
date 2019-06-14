using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionGeneratorConfigurationReader : IGeneratorConfigurationReader
    {
        #region Fields
        private readonly Action<IGeneratorConfigurationBuilderExpression> _configure;
        private readonly IFileSystemProvider _fileSystemProvider;
        #endregion

        #region Constructor
        public ActionGeneratorConfigurationReader(Action<IGeneratorConfigurationBuilderExpression> configure, IFileSystemProvider fileSystemProvider)
        {
            this._configure = configure;
            this._fileSystemProvider = fileSystemProvider;
        }
        #endregion

        #region IGeneratorConfigurationReader
        public void Read(GeneratorConfiguration configuration)
        {
            GeneratorConfigurationBuilderExpression expression = new GeneratorConfigurationBuilderExpression(configuration, this._fileSystemProvider);
            this._configure(expression);
        }
        #endregion

        #region Nested Types
        internal sealed class GeneratorConfigurationBuilderExpression : IGeneratorConfigurationBuilderExpression
        {
            #region Fields
            private readonly GeneratorConfiguration _configuration;
            private readonly IFileSystemProvider _fileSystemProvider;
            #endregion

            #region Constructor
            public GeneratorConfigurationBuilderExpression(GeneratorConfiguration configuration, IFileSystemProvider fileSystemProvider)
            {
                this._configuration = configuration;
                this._fileSystemProvider = fileSystemProvider;
            }
            #endregion

            #region IGeneratorConfigurationBuilderExpression Members
            public IGeneratorConfigurationBuilderExpression AddSource(string projectName) { return this.AddSource(projectName, null); }
            public IGeneratorConfigurationBuilderExpression AddSource(Action<IPhysicalSourceSelectionExpression> configuration) { return this.AddSource(null, configuration); }
            public IGeneratorConfigurationBuilderExpression AddSource(string projectName, Action<IPhysicalSourceSelectionExpression> configuration)
            {
                Guard.IsNotNullOrEmpty(projectName, nameof(projectName));
                PhysicalSourceConfiguration sourceConfiguration = new PhysicalSourceConfiguration(this._fileSystemProvider, projectName, false, null, null);
                PhysicalSourceConfigurationExpression expression = new PhysicalSourceConfigurationExpression(sourceConfiguration);
                configuration?.Invoke(expression);
                this._configuration.Input.Sources.Add(sourceConfiguration);
                return this;
            }

            public IGeneratorConfigurationBuilderExpression AddDacPac(string packagePath, Action<IDacPacSelectionExpression> configuration)
            {
                Guard.IsNotNull(configuration, nameof(configuration));
                DacPacSourceConfiguration sourceConfiguration = new DacPacSourceConfiguration(this._fileSystemProvider, packagePath);
                DacPacSourceConfigurationExpression expression = new DacPacSourceConfigurationExpression(sourceConfiguration);
                configuration(expression);
                this._configuration.Input.Sources.Add(sourceConfiguration);
                return this;
            }

            public IGeneratorConfigurationBuilderExpression SelectOutputWriter<TWriter>() where TWriter : IWriter { return this.SelectOutputWriter<TWriter>(null); }
            public IGeneratorConfigurationBuilderExpression SelectOutputWriter<TWriter>(Action<IOutputConfigurationExpression> configuration) where TWriter : IWriter
            {
                OutputConfigurationExpression expression = new OutputConfigurationExpression(this._configuration.Output);
                configuration?.Invoke(expression);

                this._configuration.Output.Writer = typeof(TWriter);
                return this;
            }
            #endregion
        }
        #endregion
    }
}