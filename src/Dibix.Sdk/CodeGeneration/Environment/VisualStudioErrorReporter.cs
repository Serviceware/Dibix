using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class VisualStudioErrorReporter : ErrorReporter, IErrorReporter
    {
        #region Fields
        private readonly IServiceProvider _serviceProvider;
        private readonly ErrorListProvider _errorProvider;
        private readonly DTE _dte;
        private readonly IVsSolution _solution;
        private readonly IDictionary<string, int> _currentErrors;
        #endregion

        #region Constructor
        public VisualStudioErrorReporter(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
            this._errorProvider = new ErrorListProvider(serviceProvider) { MaintainInitialTaskOrder = true };
            this._dte = (DTE)serviceProvider.GetService(typeof(DTE));
            this._solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            this._currentErrors = new Dictionary<string, int>();
        }
        #endregion

        #region Overrides
        public override bool ReportErrors()
        {
            this._currentErrors.Clear();
            this._errorProvider.Tasks.Clear();

            foreach (CompilerError error in base.Errors)
            {
                int line = error.Line;
                int column = error.Column;
                string message = error.ErrorText;
                string fileName = error.FileName;
                if (line > 0)
                    --line;

                if (column > 0)
                    --column;

                if (this._currentErrors != null && this._currentErrors.TryGetValue(message, out _))
                    continue;

                if (this._currentErrors != null)
                    this._currentErrors[message] = 1;

                //if (this.callback != null)
                //    this.callback.ErrorCallback(isWarning, message, line, column);

                ErrorTask errorTask = new ErrorTask();
                IVsHierarchy ppHierarchy = null;
                IVsSolution service = this._solution;
                ProjectItem projectItem = null;
                if (this._dte.Solution != null)
                    projectItem = this._dte.Solution.FindProjectItem(fileName);

                if (projectItem?.ContainingProject != null)
                    service.GetProjectOfUniqueName(projectItem.ContainingProject.UniqueName, out ppHierarchy);

                errorTask.Category = TaskCategory.BuildCompile;
                errorTask.Document = fileName;
                errorTask.HierarchyItem = ppHierarchy;
                errorTask.CanDelete = false;
                errorTask.Column = column;
                errorTask.Line = line;
                errorTask.Text = message;
                errorTask.ErrorCategory = error.IsWarning ? TaskErrorCategory.Warning : TaskErrorCategory.Error;
                errorTask.Navigate += this.OnNavigateError;
                this._errorProvider.Tasks.Add(errorTask);
            }

            return base.Errors.Count > 0;
        }
        #endregion

        #region Private Methods
        private void OnNavigateError(object sender, EventArgs e)
        {
            if (!(sender is ErrorTask errorTask) || String.IsNullOrEmpty(errorTask.Document) || !File.Exists(errorTask.Document))
                return;

            VsShellUtilities.OpenDocument(this._serviceProvider, errorTask.Document, Guid.Empty, out IVsUIHierarchy hierarchy, out uint _, out IVsWindowFrame windowFrame);
            if (windowFrame == null)
                return;

            errorTask.HierarchyItem = hierarchy;
            this._errorProvider.Refresh();
            VsShellUtilities.GetTextView(windowFrame)?.SetSelection(errorTask.Line, errorTask.Column, errorTask.Line, errorTask.Column);
        }
        #endregion
    }
}