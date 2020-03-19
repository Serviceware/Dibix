using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.CodeGeneration;
using EnvDTE;

namespace Dibix.Sdk.VisualStudio
{
    internal sealed class ProjectFileSystemProvider : IFileSystemProvider
    {
        #region Fields
        private readonly DTE _dte;
        private readonly IDictionary<string, Project> _projectCache;
        #endregion

        #region Properties
        public string CurrentDirectory { get; }
        #endregion

        #region Constructor
        public ProjectFileSystemProvider(IServiceProvider serviceProvider, string executingFilePath)
        {
            this._dte = (DTE)serviceProvider.GetService(typeof(DTE));
            this._projectCache = new Dictionary<string, Project>();
            this.CurrentDirectory = Path.GetDirectoryName(executingFilePath);
        }
        #endregion

        #region IFileSystemProvider Members
        public string GetPhysicalFilePath(string root, VirtualPath virtualPath)
        {
            Project project = this.GetProject(root);
            ProjectItem folder = FindItem(project.ProjectItems, virtualPath);
            string physicalFilePath = folder.Properties.GetFullPath();
            return physicalFilePath;
        }

        public IEnumerable<string> GetFiles(string root, IEnumerable<VirtualPath> include, IEnumerable<VirtualPath> exclude)
        {
            Project project = this.GetProject(root);
            ICollection<string> normalizedExclude = exclude.Select(x => (string)x).ToArray();

            foreach (VirtualPath virtualPath in include)
            {
                ProjectItems children;
                Properties properties;
                string kind;
                if (virtualPath.IsCurrent)
                {
                    children = project.ProjectItems;
                    properties = project.Properties;
                    kind = Constants.vsProjectItemKindPhysicalFolder;
                }
                else
                {
                    ProjectItem item = FindItem(project.ProjectItems, virtualPath);
                    children = item.ProjectItems;
                    properties = item.Properties;
                    kind = item.Kind;
                }

                foreach (string file in GetFiles(children, properties, kind, normalizedExclude, virtualPath))
                {
                    yield return file;
                }
            }
        }
        #endregion

        #region Private Methods
        private Project GetProject(string projectName)
        {
            if (!this._projectCache.TryGetValue(projectName, out Project project))
            {
                project = FindProject(this._dte.Solution, projectName);
                if (project.Kind != ProjectKind.SqlProj)
                    throw new InvalidOperationException($"'{project.Name}' is not a valid SQL project");

                this._projectCache.Add(projectName, project);
            }
            return project;
        }

        private static Project FindProject(Solution solution, string projectName)
        {
            Project project = solution.Projects
                                      .Cast<Project>()
                                      .SelectMany(GetProjects)
                                      .FirstOrDefault(x => x.Name == projectName);

            if (project == null)
                throw new InvalidOperationException($"Could not find project '{projectName}' in current solution");

            return project;
        }

        private static IEnumerable<Project> GetProjects(Project project)
        {
            if (project.Kind == Constants.vsProjectKindSolutionItems)
            {
                return project.ProjectItems
                              .Cast<ProjectItem>()
                              .Select(x => x.SubProject)
                              .Where(x => x != null)
                              .SelectMany(GetProjects);
            }
            return new[] { project };
        }

        private static ProjectItem FindItem(ProjectItems items, VirtualPath virtualPath)
        {
            ProjectItem item = null;
            foreach (string part in virtualPath.Segments)
            {
                item = items.Item(part);
                if (item == null)
                    break;

                items = item.ProjectItems;
            }

            if (item == null)
                throw new InvalidOperationException($"Could not find project item using path '{virtualPath.Path}'");

            return item;
        }

        private static IEnumerable<string> GetFiles(ProjectItems children, Properties properties, string itemKind, ICollection<string> normalizedExclude, VirtualPath virtualPath)
        {
            string physicalPath = properties.GetFullPath();
            switch (itemKind.ToUpperInvariant())
            {
                case Constants.vsProjectItemKindPhysicalFile:
                    yield return physicalPath;
                    break;

                case Constants.vsProjectItemKindPhysicalFolder:
                    foreach (string filePath in ScanFiles(children, physicalPath, normalizedExclude, virtualPath.IsRecursive))
                        yield return filePath;

                    break;

                default: throw new ArgumentOutOfRangeException(nameof(itemKind), itemKind, null);
            }
        }

        private static IEnumerable<string> ScanFiles(ProjectItems root, string rootDirectory, ICollection<string> exclude, bool recursive)
        {
            return from item in root.GetChildren(recursive)
                   where item.Kind == Constants.vsProjectItemKindPhysicalFile
                   let itemPath = item.Properties.GetFullPath()
                   let relativePath = itemPath.Substring(rootDirectory.Length).TrimStart(Path.DirectorySeparatorChar)
                   where !exclude.Any(x => relativePath.StartsWith(x, StringComparison.OrdinalIgnoreCase))
                   where item.Name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
                   orderby item.Name
                   select itemPath;
        }
        #endregion
    }
}
