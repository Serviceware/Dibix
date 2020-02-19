using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Dibix.Sdk.CodeGeneration;
using EnvDTE;
using Microsoft.VisualStudio.TextTemplating;

namespace Dibix.Sdk.VisualStudio
{
    internal static class TextTemplateCodeGenerationModelLoader
    {
        #region Fields
        private const string ProjectDefaultNamespaceKey = "projectDefaultNamespace";
        #endregion

        #region Methods
        public static CodeGenerationModel Create(ITextTemplatingEngineHost textTemplatingEngineHost, IServiceProvider serviceProvider, IErrorReporter errorReporter, Action<ICodeGeneratorConfigurationExpression> configure)
        {
            IFileSystemProvider fileSystemProvider = new ProjectFileSystemProvider(serviceProvider, textTemplatingEngineHost.TemplateFile);
            IContractResolver contractResolver = new CodeElementContractResolver(serviceProvider, textTemplatingEngineHost.TemplateFile);
            IAssemblyLocator assemblyLocator = new ProjectReferenceAssemblyLocator(serviceProvider, textTemplatingEngineHost.TemplateFile);
            IContractResolverFacade contractResolverFacade = new ContractResolverFacade(assemblyLocator);
            contractResolverFacade.RegisterContractResolver(contractResolver, 0);

            CodeGenerationModel model = new CodeGenerationModel(CodeGeneratorCompatibilityLevel.Legacy);

            // User configuration
            CodeGeneratorConfigurationExpression expression = new CodeGeneratorConfigurationExpression(model, fileSystemProvider);
            configure(expression);

            // Collect artifacts from input configuration
            foreach (InputSourceConfiguration input in expression.Inputs)
            {
                if (input.Parser == null)
                    input.Parser = typeof(SqlStoredProcedureParser);

                if (input.Formatter == null)
                    input.Formatter = typeof(TakeSourceSqlStatementFormatter);

                input.Collect(model, contractResolverFacade, errorReporter);
            }

            // Apply configuration defaults
            if (model.RootNamespace == null)
                model.RootNamespace = GetProjectDefaultNamespace(textTemplatingEngineHost, serviceProvider);

            if (model.DefaultClassName == null)
                model.DefaultClassName = Path.GetFileNameWithoutExtension(textTemplatingEngineHost.TemplateFile);

            if (model.CommandTextFormatting == default)
                model.CommandTextFormatting = CommandTextFormatting.Singleline;

            return model;
        }
        #endregion

        #region Private Methods
        private static string GetProjectDefaultNamespace(ITextTemplatingEngineHost templatingEngineHost, IServiceProvider serviceProvider)
        {
            string defaultNamespace = templatingEngineHost.ResolveParameterValue("-", "-", ProjectDefaultNamespaceKey);

            DTE dte = (DTE)serviceProvider.GetService(typeof(DTE));
            Project project = VisualStudioExtensions.GetContainingProject(dte, templatingEngineHost.TemplateFile);
            string projectDirectory = project.Properties.GetFullPath().TrimEnd('\\');
            string currentDirectory = Path.GetDirectoryName(templatingEngineHost.TemplateFile);
            string virtualPath = currentDirectory.Substring(projectDirectory.Length);

            string @namespace;

            // Append folders to namespace
            if (virtualPath.Length > 0)
            {
                virtualPath = virtualPath.Replace('\\', '.');
                @namespace = String.Concat(defaultNamespace, virtualPath);
            }
            else
                @namespace = defaultNamespace;

            return @namespace;
        }
        #endregion

        #region Nested Types
        private sealed class CodeGeneratorConfigurationExpression : ICodeGeneratorConfigurationExpression
        {
            #region Fields
            private readonly CodeGenerationModel _model;
            private readonly IFileSystemProvider _fileSystemProvider;
            #endregion

            #region Properties
            public ICollection<InputSourceConfiguration> Inputs { get; }
            #endregion

            #region Constructor
            public CodeGeneratorConfigurationExpression(CodeGenerationModel model, IFileSystemProvider fileSystemProvider)
            {
                this._model = model;
                this._fileSystemProvider = fileSystemProvider;
                this.Inputs = new Collection<InputSourceConfiguration>();
            }
            #endregion

            #region IGeneratorConfigurationBuilderExpression Members
            public ICodeGeneratorConfigurationExpression AddSource(string projectName, Action<IPhysicalSourceSelectionExpression> configuration)
            {
                Guard.IsNotNullOrEmpty(projectName, nameof(projectName));
                PhysicalSourceConfiguration sourceConfiguration = new PhysicalSourceConfiguration(this._fileSystemProvider, projectName);
                PhysicalSourceConfigurationExpression expression = new PhysicalSourceConfigurationExpression(sourceConfiguration);
                configuration?.Invoke(expression);
                this.Inputs.Add(sourceConfiguration);
                return this;
            }

            public ICodeGeneratorConfigurationExpression AddDacPac(string packagePath, Action<IDacPacSelectionExpression> configuration)
            {
                Guard.IsNotNull(configuration, nameof(configuration));
                DacPacSourceConfiguration sourceConfiguration = new DacPacSourceConfiguration(this._fileSystemProvider, packagePath);
                DacPacSourceConfigurationExpression expression = new DacPacSourceConfigurationExpression(sourceConfiguration);
                configuration(expression);
                this.Inputs.Add(sourceConfiguration);
                return this;
            }

            public ICodeGeneratorConfigurationExpression SelectOutputWriter<TWriter>(Action<IOutputConfigurationExpression> configuration)
            {
                OutputConfigurationExpression expression = new OutputConfigurationExpression(this._model);
                configuration?.Invoke(expression);
                return this;
            }
            #endregion
        }
        #endregion
    }
}