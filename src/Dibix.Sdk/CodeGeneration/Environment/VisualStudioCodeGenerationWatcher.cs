using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using VSLangProj;
using Constants = EnvDTE.Constants;
using SolutionEvents = Microsoft.VisualStudio.Shell.Events.SolutionEvents;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class VisualStudioCodeGenerationWatcher
    {
        private static VisualStudioCodeGenerationFileEvents _fileEvents;
        private static IServiceProvider _serviceProvider;
        private static IErrorReporter _errorReporter;

        public static void Initialize(IServiceProvider serviceProvider, IErrorReporter errorReporter)
        {
            _serviceProvider = serviceProvider;
            _errorReporter = errorReporter;
            SolutionEvents.OnAfterOpenProject += OnAfterOpenProject;
        }

        private static void OnAfterOpenProject(object sender, OpenProjectEventArgs e)
        {
            e.Hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object objProj);
            Project project = (Project)objProj;

            // Find items that have the custom tool for the code generator assigned
            IEnumerable<ProjectItem> files = project.ProjectItems
                                                    .GetChildren()
                                                    .Where(projectItem => projectItem.Kind == Constants.vsProjectItemKindPhysicalFile
                                                                       && Equals(projectItem.Properties.Item(nameof(FileProperties.CustomTool))?.Value, "Dibix"));

            foreach (ProjectItem projectItem in files)
            {
                if (_fileEvents == null)
                    _fileEvents = new VisualStudioCodeGenerationFileEvents(_serviceProvider, _errorReporter);

                _fileEvents.Subscribe(projectItem);
            }
        }
    }
}
