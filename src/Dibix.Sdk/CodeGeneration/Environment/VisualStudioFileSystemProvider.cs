using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class VisualStudioFileSystemProvider : IFileSystemProvider
    {
        #region Fields
        private readonly DTE _dte;
        private readonly IDictionary<string, Project> _projectCache;
        #endregion

        #region Properties
        public string CurrentDirectory { get; }
        #endregion

        #region Constructor
        public VisualStudioFileSystemProvider(IServiceProvider serviceProvider, string executingFilePath)
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
                if (virtualPath.IsCurrent)
                {
                    children = project.ProjectItems;
                    properties = project.Properties;
                }
                else
                {
                    ProjectItem item = FindItem(project.ProjectItems, virtualPath);
                    children = item.ProjectItems;
                    properties = item.Properties;
                }

                string physicalPath = properties.GetFullPath();
                if (File.Exists(physicalPath))
                {
                    yield return physicalPath;
                }
                else
                {
                    foreach (string filePath in ScanFiles(children, physicalPath, normalizedExclude, virtualPath.IsRecursive))
                        yield return filePath;
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

        private static IEnumerable<string> ScanFiles(ProjectItems root, string rootDirectory, IEnumerable<string> exclude, bool recursive)
        {
            IEnumerable<string> physicalFilePaths = root.GetChildren(recursive)
                                                        .Where(x => MatchFile(x, rootDirectory, exclude))
                                                        .OrderBy(x => x.Name)
                                                        .Select(x => x.FileNames[0]);

            return physicalFilePaths;
        }

        private static bool MatchFile(ProjectItem item, string rootDirectory, IEnumerable<string> exclude)
        {
            if (item.Kind != Constants.vsProjectItemKindPhysicalFile)
                return false;

            string itemPath = item.Properties.GetFullPath();
            string relativePath = itemPath.Substring(rootDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
            if (exclude.Any(x => relativePath.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
                return false;

            return item.Name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase);
        }
        #endregion
    }
}
