using System;
using System.Collections.Generic;
using EnvDTE;

namespace Dibix.Sdk
{
    internal static class VisualStudioExtensions
    {
        public static Project GetContainingProject(DTE dte, string executingFilePath)
        {
            ProjectItem item = dte.Solution.FindProjectItem(executingFilePath);
            if (item == null)
                throw new InvalidOperationException("Can't locate currently executing file");

            if (item.ContainingProject == null)
                throw new InvalidOperationException("Can't locate project of currently executing file");

            return item.ContainingProject;
        }


        public static IEnumerable<ProjectItem> GetChildren(this ProjectItems projectItems, bool recursive)
        {
            foreach (ProjectItem item in projectItems)
            {
                if (recursive && item.ProjectItems.Count > 0)
                {
                    foreach (ProjectItem subItem in GetChildren(item.ProjectItems, true))
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
    }
}
