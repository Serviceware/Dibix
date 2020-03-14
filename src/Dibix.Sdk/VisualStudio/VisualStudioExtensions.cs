using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using VSLangProj;

namespace Dibix.Sdk.VisualStudio
{
    internal static class VisualStudioExtensions
    {
        public static string GetFullPath(this Properties properties) => (string)properties.Item(nameof(FileProperties.FullPath)).Value;

        public static Project GetContainingProject(DTE dte, string executingFilePath)
        {
            ProjectItem item = dte.Solution.FindProjectItem(executingFilePath);
            if (item == null)
                throw new InvalidOperationException("Can't locate currently executing file");

            if (item.ContainingProject == null)
                throw new InvalidOperationException("Can't locate project of currently executing file");

            return item.ContainingProject;
        }
        
        public static string GetOutputPath(this Project project)
        {
            const string outputFileNameKey = "OutputFileName";
            string outputDirectory = GetOutputDirectory(project);
            string fileName = GetProperty(project, outputFileNameKey);
            string outputPath = Path.GetFullPath(Path.Combine(outputDirectory, fileName));
            return outputPath;
        }

        public static IEnumerable<string> GetAssemblyReferences(this Project project)
        {
            VSProject vsProject = (VSProject)project.Object;
            return vsProject.References.Cast<Reference>().Select(x => x.Path);
        }

        public static IEnumerable<ProjectItem> GetChildren(this ProjectItems projectItems) => GetChildren(projectItems, true);
        public static IEnumerable<ProjectItem> GetChildren(this ProjectItems projectItems, bool recursive)
        {
            if (projectItems == null)
                yield break;

            foreach (ProjectItem item in projectItems)
            {
                if (recursive)
                {
                    foreach (ProjectItem subItem in GetChildren(item.ProjectItems, true))
                    {
                        yield return subItem;
                    }
                }

                yield return item;
            }
        }

        private static string GetOutputDirectory(Project project)
        {
            const string outputDirectoryKey = "OutputPath";
            string projectDirectory = GetProperty(project, nameof(FileProperties.FullPath));
            string outputDirectory = GetConfigurationProperty(project, outputDirectoryKey);
            string path = Path.GetFullPath(Path.Combine(projectDirectory, outputDirectory));
            return path;
        }

        private static string GetConfigurationProperty(Project project, string key) => GetProperty(project.ConfigurationManager.ActiveConfiguration.Properties, key);
        
        private static string GetProperty(Project project, string key) => GetProperty(project.Properties, key);
        private static string GetProperty(Properties properties, string key)
        {
            string value = properties.Cast<Property>()
                                     .Where(x => x.Name == key)
                                     .Select(x => x.Value as string)
                                     .FirstOrDefault();

            if (String.IsNullOrEmpty(value))
                throw new InvalidOperationException($"Could not determine '{key}' property of project");

            return value;
        }
    }
}
