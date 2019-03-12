using System;
using System.IO;
using Dibix.Sdk.CodeGeneration;
using EnvDTE;
using Microsoft.VisualStudio.TextTemplating;

namespace Dibix.Sdk.VisualStudio
{
    internal sealed class TextTemplateCodeGenerationContext : ICodeGenerationContext
    {
        #region Fields
        private const string ProjectDefaultNamespaceKey = "projectDefaultNamespace";
        #endregion

        #region Properties
        public GeneratorConfiguration Configuration { get; }
        public string Namespace { get; }
        public string ClassName { get; }
        public ITypeLoaderFacade TypeLoaderFacade { get; }
        public IErrorReporter ErrorReporter { get; }
        #endregion

        #region Constructor
        public TextTemplateCodeGenerationContext(GeneratorConfiguration configuration, ITypeLoaderFacade typeLoaderFacade, IErrorReporter errorReporter, ITextTemplatingEngineHost textTemplatingEngineHost, IServiceProvider serviceProvider)
        {
            this.Configuration = configuration;
            this.TypeLoaderFacade = typeLoaderFacade;
            this.ErrorReporter = errorReporter;
            this.Namespace = GetProjectDefaultNamespace(textTemplatingEngineHost, serviceProvider);
            this.ClassName = Path.GetFileNameWithoutExtension(textTemplatingEngineHost.TemplateFile);
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
    }
}