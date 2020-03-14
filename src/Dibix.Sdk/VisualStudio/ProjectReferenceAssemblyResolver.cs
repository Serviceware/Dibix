using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dibix.Sdk.CodeGeneration;
using EnvDTE;

namespace Dibix.Sdk.VisualStudio
{
    internal sealed class ProjectReferenceAssemblyResolver : AssemblyResolver, IReferencedAssemblyProvider
    {
        #region Fields
        private readonly ICollection<string> _assemblyReferenceCache;
        private readonly IDictionary<string, string> _assemblyLocationMap;
        private readonly Project _currentProject;
        #endregion

        #region Properties
        public IEnumerable<Assembly> ReferencedAssemblies => this._assemblyReferenceCache.Select(base.LoadAssembly);
        #endregion

        #region Constructor
        public ProjectReferenceAssemblyResolver(IServiceProvider serviceProvider, string executingFilePath)
        {
            DTE dte = (DTE)serviceProvider.GetService(typeof(DTE));
            this._currentProject = VisualStudioExtensions.GetContainingProject(dte, executingFilePath);
            this._assemblyReferenceCache = this._currentProject.GetAssemblyReferences().ToArray();
            this._assemblyLocationMap = this.ScanAssemblyLocations();
        }
        #endregion

        #region Overrides
        protected override bool TryGetAssemblyLocation(string assemblyName, out string assemblyPath) => this._assemblyLocationMap.TryGetValue(new AssemblyName(assemblyName).Name, out assemblyPath);
        #endregion

        #region Private Methods
        private IDictionary<string, string> ScanAssemblyLocations() => Enumerable.Repeat(this._currentProject.GetOutputPath(), 1)
                                                                                 .Union(this._assemblyReferenceCache)
                                                                                 .ToDictionary(Path.GetFileNameWithoutExtension);
        #endregion
    }
}