using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EnvDTE;
using Microsoft.VisualStudio.TextTemplating;
using VSLangProj;

namespace Dibix.Sdk.CodeGeneration
{
    public class VisualStudioExecutionEnvironment : IExecutionEnvironment, IFileSystemProvider, ITypeLoader
    {
        #region Fields
        private const string VsProjectKindSolutionFolder = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";
        private const string ProjectDefaultNamespaceKey = "projectDefaultNamespace";
        private const string PhysicalPathKey = "FullPath";
        private const string OutputDirectoryKey = "OutputPath";
        private const string OutputFileNameKey = "OutputFileName";
        private readonly ITextTemplatingEngineHost _host;
        private readonly DTE _dte;
        private readonly IDictionary<string, Project> _projectCache;
        private readonly CurrentProjectInfo _currentProject;
        private readonly CompilerErrorCollection _errors;
        private readonly Lazy<IDictionary<string, string>> _assemblyLocationLookupAccessor;
        private readonly Lazy<ICollection<CodeElement>> _codeItemAccessor;
        #endregion

        #region Constructor
        public VisualStudioExecutionEnvironment(ITextTemplatingEngineHost host, IServiceProvider serviceProvider)
        {
            this._host = host;
            this._dte = (DTE)serviceProvider.GetService(typeof(DTE));
            this._projectCache = new Dictionary<string, Project>();
            this._currentProject = this.GetCurrentProject();
            this._errors = new CompilerErrorCollection();
            this._assemblyLocationLookupAccessor = new Lazy<IDictionary<string, string>>(this.BuildAssemblyLocationLookup);
            this._codeItemAccessor = new Lazy<ICollection<CodeElement>>(this.GetCodeItems);
        }
        #endregion

        #region IExecutionEnvironment Members
        public string GetCurrentDirectory()
        {
            return Path.GetDirectoryName(this._host.TemplateFile);
        }

        public string GetProjectName()
        {
            return this._currentProject.Project.Name;
        }

        public string GetProjectDefaultNamespace()
        {
            string defaultNamespace = this._host.ResolveParameterValue("-", "-", ProjectDefaultNamespaceKey);

            string projectDirectory = this._currentProject.Project.Properties.Item("FullPath").Value.ToString().TrimEnd('\\');
            string currentDirectory = this.GetCurrentDirectory();
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

        public string GetClassName()
        {
            ProjectItem item = this._dte.Solution.FindProjectItem(this._host.TemplateFile);
            string className = Path.GetFileNameWithoutExtension(item.Name);
            return className;
        }

        public void VerifyProject(string projectName)
        {
            Project project = this.GetProject(projectName);
            if (project.Kind != ProjectKind.SqlProj)
                throw new InvalidOperationException($"'{project.Name}' is not a valid SQL project");
        }

        public bool TryGetAssemblyLocation(string assemblyName, out string assemblyPath)
        {
            return this._assemblyLocationLookupAccessor.Value.TryGetValue(new AssemblyName(assemblyName).Name, out assemblyPath);
        }

        public void RegisterError(string fileName, int line, int column, string errorNumber, string errorText)
        {
            // Apparently errors are reported with distinct description, even though a different position is supplied
            // To make it work we append the position to the message
            errorText = String.Concat(errorText, ZeroWidthUtility.MaskText($" ({line},{column})"));
            this._errors.Add(new CompilerError(fileName, line, column, errorNumber, errorText));
        }

        public bool ReportErrors()
        {
            this._host.LogErrors(this._errors);
            return this._errors.Count > 0;
        }
        #endregion

        #region IFileSystemProvider Members
        public string GetPhysicalFilePath(string projectName, VirtualPath virtualPath)
        {
            Project project = this.GetProject(projectName);
            ProjectItem folder = FindItem(project.ProjectItems, virtualPath);
            string physicalFilePath = (string)folder.Properties.Item(PhysicalPathKey).Value;
            return physicalFilePath;
        }

        public IEnumerable<string> GetFiles(string projectName, IEnumerable<VirtualPath> include, IEnumerable<VirtualPath> exclude)
        {
            Project project = this.GetProject(projectName);
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

                string physicalPath = (string)properties.Item(PhysicalPathKey).Value;
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

        #region ITypeLoader Members
        TypeInfo ITypeLoader.LoadType(IExecutionEnvironment environment, TypeName typeName, Action<string> errorHandler)
        {
            CodeElement codeItem = this._codeItemAccessor.Value.FirstOrDefault(x => x.FullName == typeName.NormalizedTypeName);
            if (codeItem == null)
            {
                errorHandler($"Could not resolve type '{typeName}'. Looking in current project.");
                return null;
            }

            TypeInfo type = CreateTypeInfo(typeName, codeItem);
            return type;
        }
        #endregion

        #region Private Methods
        private CurrentProjectInfo GetCurrentProject()
        {
            ProjectItem item = this._dte.Solution.FindProjectItem(this._host.TemplateFile);
            if (item == null)
                throw new InvalidOperationException("Can't locate currently executing T4 file");

            if (item .ContainingProject== null)
                throw new InvalidOperationException("Can't locate project of currently executing T4 file");

            Project project = item.ContainingProject;
            return new CurrentProjectInfo(item, project);
        }

        private IDictionary<string, string> BuildAssemblyLocationLookup()
        {
            return new[] { this.GetProjectOutputPath() }.Union(this.GetProjectAssemblyReferences()).ToDictionary(Path.GetFileNameWithoutExtension);
        }

        private ICollection<CodeElement> GetCodeItems()
        {
            ICollection<CodeElement> codeItems = TraverseProjectItems(this._currentProject.Project.ProjectItems, true).Where(item => item.FileCodeModel != null)
                .SelectMany(item => TraverseTypes(item.FileCodeModel.CodeElements, vsCMElement.vsCMElementClass, vsCMElement.vsCMElementEnum))
                .ToArray();

            return codeItems;
        }

        private string GetOutputDirectory()
        {
            string projectDirectory = GetProjectProperty(this._currentProject.Project, PhysicalPathKey);
            string outputDirectory = GetProjectConfigurationProperty(this._currentProject.Project, OutputDirectoryKey);
            string path = Path.GetFullPath(Path.Combine(projectDirectory, outputDirectory));
            return path;
        }

        private string GetProjectOutputPath()
        {
            string outputDirectory = this.GetOutputDirectory();
            string fileName = GetProjectProperty(this._currentProject.Project, OutputFileNameKey);
            string outputPath = Path.GetFullPath(Path.Combine(outputDirectory, fileName));
            return outputPath;
        }

        private static string GetProjectConfigurationProperty(Project project, string key)
        {
            return GetProjectProperty(project.ConfigurationManager.ActiveConfiguration.Properties, key);
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

        private IEnumerable<string> GetProjectAssemblyReferences()
        {
            VSProject vsProject = (VSProject)this._currentProject.Project.Object;
            return vsProject.References.Cast<Reference>().Select(x => x.Path);
        }

        private static IEnumerable<CodeElement> TraverseTypes(CodeElements parent, params vsCMElement[] kinds)
        {
            foreach (CodeElement elem in parent)
            {
                if (elem.Kind == vsCMElement.vsCMElementNamespace)
                {
                    foreach (CodeElement element in TraverseTypes(((CodeNamespace)elem).Members, kinds))
                    {
                        yield return element;
                    }
                }

                if (kinds.Contains(elem.Kind))
                {
                    yield return elem;
                }
            }
        }

        private Project GetProject(string projectName)
        {
            if (!this._projectCache.TryGetValue(projectName, out Project project))
            {
                project = FindProject(this._dte.Solution, projectName);
                this._projectCache.Add(projectName, project);
            }
            return project;
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
                throw new InvalidOperationException($"Could not find project item using path '{virtualPath}'");

            return item;
        }

        private static IEnumerable<string> ScanFiles(ProjectItems root, string rootDirectory, IEnumerable<string> exclude, bool recursive)
        {
            IEnumerable<string> physicalFilePaths = TraverseProjectItems(root, recursive)
                .Where(x => MatchFile(x, rootDirectory, exclude))
                .OrderBy(x => x.Name)
                .Select(x => x.FileNames[0]);

            return physicalFilePaths;
        }

        private static bool MatchFile(ProjectItem item, string rootDirectory, IEnumerable<string> exclude)
        {
            if (item.Kind != Constants.vsProjectItemKindPhysicalFile)
                return false;

            string itemPath = (string)item.Properties.Item(PhysicalPathKey).Value;
            string relativePath = itemPath.Substring(rootDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
            if (exclude.Any(x => relativePath.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
                return false;

            return item.Name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<ProjectItem> TraverseProjectItems(ProjectItems projectItems, bool recursive)
        {
            foreach (ProjectItem item in projectItems)
            {
                if (recursive && item.ProjectItems.Count > 0)
                {
                    foreach (ProjectItem subItem in TraverseProjectItems(item.ProjectItems, true))
                    {
                        yield return subItem;
                    }
                }

                else
                {
                    yield return item;
                }
            }
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
            if (project.Kind == VsProjectKindSolutionFolder)
            {
                return project.ProjectItems
                              .Cast<ProjectItem>()
                              .Select(x => x.SubProject)
                              .Where(x => x != null)
                              .SelectMany(GetProjects);
            }
            return new[] { project };
        }

        private static TypeInfo CreateTypeInfo(TypeName typeName, CodeElement element)
        {
            bool isPrimitiveType = element.Kind == vsCMElement.vsCMElementEnum;
            TypeInfo type = new TypeInfo(typeName, isPrimitiveType);

            CodeClass @class = element as CodeClass;
            if (@class != null)
            {
                IEnumerable<string> properties = TraverseProperties(@class);
                type.Properties.AddRange(properties);
            }

            return type;
        }

        private static IEnumerable<string> TraverseProperties(CodeClass @class)
        {
            foreach (CodeElement element in @class.Members.Cast<CodeElement>().Where(element => element.Kind == vsCMElement.vsCMElementProperty))
            {
                yield return element.Name;
            }

            foreach (string property in @class.Bases.OfType<CodeClass>().SelectMany(TraverseProperties))
            {
                yield return property;
            }
        }
        #endregion

        #region Nested Types
        private class CurrentProjectInfo
        {
            public ProjectItem TemplateFile { get; }
            public Project Project { get; }

            public CurrentProjectInfo(ProjectItem templateFile, Project project)
            {
                this.TemplateFile = templateFile;
                this.Project = project;
            }
        }
        #endregion
    }
}