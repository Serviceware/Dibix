using System;
using System.Collections.Generic;
using EnvDTE;
using VSLangProj;

namespace Dibix.Sdk
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
    }
}
