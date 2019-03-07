using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EnvDTE;
using VSLangProj;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class VisualStudioAssemblyLocator : IAssemblyLocator
    {
        #region Fields
        private const string OutputDirectoryKey = "OutputPath";
        private const string OutputFileNameKey = "OutputFileName";
        private readonly Lazy<IDictionary<string, string>> _assemblyLocationLookupAccessor;
        private readonly Project _currentProject;
        #endregion

        #region Constructor
        public VisualStudioAssemblyLocator(IServiceProvider serviceProvider, string executingFilePath)
        {
            DTE dte = (DTE)serviceProvider.GetService(typeof(DTE));
            this._assemblyLocationLookupAccessor = new Lazy<IDictionary<string, string>>(this.BuildAssemblyLocationLookup);
            this._currentProject = VisualStudioExtensions.GetContainingProject(dte, executingFilePath);
        }
        #endregion

        #region IAssemblyLocator Members
        public bool TryGetAssemblyLocation(string assemblyName, out string assemblyPath)
        {
            return this._assemblyLocationLookupAccessor.Value.TryGetValue(new AssemblyName(assemblyName).Name, out assemblyPath);
        }
        #endregion

        #region Private Methods
        private IDictionary<string, string> BuildAssemblyLocationLookup()
        {
            return new[] { this.GetProjectOutputPath() }.Union(this.GetProjectAssemblyReferences()).ToDictionary(Path.GetFileNameWithoutExtension);
        }

        private string GetProjectOutputPath()
        {
            string outputDirectory = this.GetOutputDirectory();
            string fileName = GetProjectProperty(this._currentProject, OutputFileNameKey);
            string outputPath = Path.GetFullPath(Path.Combine(outputDirectory, fileName));
            return outputPath;
        }

        private static string GetProjectProperty(Project project, string key)
        {
            return GetProjectProperty(project.Properties, key);
        }

        private static string GetProjectProperty(Properties properties, string key)
        {
            string value = properties.Cast<Property>()
                                     .Where(x => x.Name == key)
                                     .Select(x => x.Value as string)
                                     .FirstOrDefault();

            if (String.IsNullOrEmpty(value))
                throw new InvalidOperationException($"Could not determine '{key}' property of project");

            return value;
        }

        private static string GetProjectConfigurationProperty(Project project, string key)
        {
            return GetProjectProperty(project.ConfigurationManager.ActiveConfiguration.Properties, key);
        }

        private string GetOutputDirectory()
        {
            string projectDirectory = GetProjectProperty(this._currentProject, nameof(FileProperties.FullPath));
            string outputDirectory = GetProjectConfigurationProperty(this._currentProject, OutputDirectoryKey);
            string path = Path.GetFullPath(Path.Combine(projectDirectory, outputDirectory));
            return path;
        }

        private IEnumerable<string> GetProjectAssemblyReferences()
        {
            VSProject vsProject = (VSProject)this._currentProject.Object;
            return vsProject.References.Cast<Reference>().Select(x => x.Path);
        }
        #endregion
    }
}