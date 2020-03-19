using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class ErrorReporter : IErrorReporter
    {
        #region Fields
        private bool _hasReportedErrors;
        #endregion

        #region Properties
        public bool HasErrors
        {
            get
            {
                if (!this._hasReportedErrors && this.Errors.Any())
                {
                    this.ReportErrors();
                    this._hasReportedErrors = true;
                }
                return this.Errors.Any();
            }
        }
        protected ICollection<Error> Errors { get; }
        #endregion

        #region Constructor
        protected ErrorReporter()
        {
            this.Errors = new Collection<Error>();
        }
        #endregion

        #region IErrorReporter Members
        public void RegisterError(string fileName, int line, int column, string errorNumber, string errorText)
        {
            this.Errors.Add(new Error(fileName, line, column, errorNumber, errorText));
        }
        #endregion

        #region Protected Methods
        protected abstract void ReportErrors();
        #endregion
    }
}