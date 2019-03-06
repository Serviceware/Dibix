using System;
using System.CodeDom.Compiler;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class ErrorReporter : IErrorReporter
    {
        #region Properties
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
            // Apparently errors are reported with distinct description, even though a different position is supplied
            // To make it work we append the position to the message
            errorText = String.Concat(errorText, ZeroWidthUtility.MaskText($" ({line},{column})"));
            this.Errors.Add(new CompilerError(fileName, line, column, errorNumber, errorText));
        }

        public abstract bool ReportErrors();
        #endregion
    }
}