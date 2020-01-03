﻿using System.CodeDom.Compiler;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class ErrorReporter : IErrorReporter
    {
        #region Fields
        private bool _hasReportedErrors = false;
        #endregion

        #region Properties
        public bool HasErrors
        {
            get
            {
                if (!this._hasReportedErrors || !this.Errors.HasErrors)
                {
                    this.ReportErrors();
                    this._hasReportedErrors = true;
                }
                return this.Errors.HasErrors;
            }
        }
        protected CompilerErrorCollection Errors { get; }
        #endregion

        #region Constructor
        protected ErrorReporter()
        {
            this.Errors = new CompilerErrorCollection();
        }
        #endregion

        #region IErrorReporter Members
        public void RegisterError(string fileName, int line, int column, string errorNumber, string errorText)
        {
            this.Errors.Add(new CompilerError(fileName, line, column, errorNumber, errorText));
        }
        #endregion

        #region Protected Methods
        protected abstract void ReportErrors();
        #endregion
    }
}