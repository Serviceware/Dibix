using System.CodeDom.Compiler;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class ErrorReporter : IErrorReporter
    {
        #region Properties
        public bool HasErrors => this.Errors.HasErrors;
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

        public abstract bool ReportErrors();
        #endregion
    }
}