using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dibix.Sdk.CodeGeneration;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VSLangProj;

namespace Dibix.Sdk.VisualStudio
{
    internal sealed class CodeGenerationFileEvents : IVsRunningDocTableEvents
    {
        #region Fields
        private readonly IServiceProvider _serviceProvider;
        private readonly IErrorReporter _errorReporter;
        private readonly RunningDocumentTable _runningDocumentTable;
        private readonly IDictionary<string, ICollection<VSProjectItem>> _map;
        #endregion

        #region Constructor
        public CodeGenerationFileEvents(IServiceProvider serviceProvider, IErrorReporter errorReporter)
        {
            this._serviceProvider = serviceProvider;
            this._errorReporter = errorReporter;
            this._runningDocumentTable = new RunningDocumentTable(serviceProvider);
            this._runningDocumentTable.Advise(this);
            this._map = new Dictionary<string, ICollection<VSProjectItem>>();
        }
        #endregion

        #region IVsRunningDocTableEvents Members
        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) => VSConstants.S_OK;

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame) => VSConstants.S_OK;

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;

        public int OnAfterSave(uint docCookie)
        {
            string filePath = this._runningDocumentTable.GetDocumentInfo(docCookie).Moniker;

            if (this._map.TryGetValue(filePath, out ICollection<VSProjectItem> customTools))
            {
                foreach (VSProjectItem customTool in customTools)
                {
                    customTool.RunCustomTool();
                }
            }

            return VSConstants.S_OK;
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame) => VSConstants.S_OK;

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;
        #endregion

        #region Public Methods
        public void Subscribe(ProjectItem projectItem)
        {
            VSProjectItem vsProjectItem = (VSProjectItem)projectItem.Object;
            string executingFilePath = projectItem.Properties.GetFullPath();
            GeneratorConfiguration configuration = GeneratorConfigurationBuilder.FromVisualStudio(this._serviceProvider, executingFilePath, this._errorReporter)
                                                                                .LoadJson();

            IEnumerable<string> files = configuration.Input.Sources.OfType<IPhysicalFileSelection>().SelectMany(x => x.Files);
            foreach (string file in files)
            {
                if (!this._map.TryGetValue(file, out ICollection<VSProjectItem> customTools))
                {
                    customTools = new Collection<VSProjectItem>();
                    this._map.Add(file, customTools);
                }

                customTools.Add(vsProjectItem);
            }
        }
        #endregion
    }
}