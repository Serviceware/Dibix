using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using VSLangProj;
using Constants = EnvDTE.Constants;
using SolutionEvents = Microsoft.VisualStudio.Shell.Events.SolutionEvents;

namespace Dibix.Sdk.CodeGeneration
{
    public class RunningDocTableEvents : IVsRunningDocTableEvents3
    {
        #region Properties

        private RunningDocumentTable mRunningDocumentTable;
        private DTE mDte;
        private readonly IDictionary<string, ICollection<string>> _map;

        public delegate void OnBeforeSaveHandler(object sender, Document document);
        public event OnBeforeSaveHandler BeforeSave;

        #endregion

        #region Constructor

        public RunningDocTableEvents(IServiceProvider serviceProvider)
        {
            _map = new Dictionary<string, ICollection<string>>();
            mDte = (DTE)serviceProvider.GetService(typeof(DTE));
            mRunningDocumentTable = new RunningDocumentTable(serviceProvider);
            mRunningDocumentTable.Advise(this);
        }

        #endregion

        #region IVsRunningDocTableEvents3 implementation

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterSave(uint docCookie)
        {
            if (null == BeforeSave)
                return VSConstants.S_OK;

            var document = FindDocumentByCookie(docCookie);
            if (null == document)
                return VSConstants.S_OK;

            BeforeSave(this, FindDocumentByCookie(docCookie));
            return VSConstants.S_OK;
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeSave(uint docCookie)
        {
            return VSConstants.S_OK;
        }

        #endregion

        #region Private Methods

        private Document FindDocumentByCookie(uint docCookie)
        {
            var documentInfo = mRunningDocumentTable.GetDocumentInfo(docCookie);
            return mDte.Documents.Cast<Document>().FirstOrDefault(doc => doc.FullName == documentInfo.Moniker);
        }

        #endregion

        public void Subscribe(ProjectItem projectItem)
        {
            string fullPath = nameof(FileProperties.FullPath);
            //new JsonSqlAccessorGeneratorConfigurationReader(new VisualStudioExecutionEnvironment(new CodeGeneratorContext(fullPath, null, ), )
            //this._map.Add(projectItem.Properties.Item(nameof(FileProperties.FullPath)), )
        }
    }
    public static class VisualStudioCodeGenerationSubscriber
    {
        private static RunningDocTableEvents _fileEvents;
        private static IServiceProvider _serviceProvider;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            SolutionEvents.OnAfterOpenProject += SolutionEvents_OnAfterOpenProject;
        }

        private static void SolutionEvents_OnAfterOpenProject(object sender, OpenProjectEventArgs e)
        {
            e.Hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object objProj);
            Project project = (Project)objProj;
            if (project.Kind == Constants.vsProjectKindSolutionItems)
                return;

            Debug.WriteLine($"[DIBIX] Opened project: {project.FullName} - {project.Kind}");
            foreach (ProjectItem projectItem in TraverseProjectItems(project.ProjectItems))
            {
                string customTool = projectItem.Properties.Item(nameof(FileProperties.CustomTool))?.Value as string;
                if (Equals(customTool, "Dibix"))
                {
                    if (_fileEvents == null)
                    {
                        _fileEvents = new RunningDocTableEvents(_serviceProvider);
                    }

                    _fileEvents.Subscribe(projectItem);
                }
            }
        }

        private static IEnumerable<ProjectItem> TraverseProjectItems(ProjectItems projectItems)
        {
            foreach (ProjectItem item in projectItems)
            {
                if (item.ProjectItems.Count > 0)
                {
                    foreach (ProjectItem subItem in TraverseProjectItems(item.ProjectItems))
                    {
                        yield return subItem;
                    }
                }

                yield return item;
            }
        }
    }
}
