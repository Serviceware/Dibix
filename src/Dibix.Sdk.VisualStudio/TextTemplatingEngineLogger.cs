using System;
using System.CodeDom.Compiler;
using Microsoft.VisualStudio.TextTemplating;

namespace Dibix.Sdk.VisualStudio
{
    internal sealed class TextTemplatingEngineLogger : ILogger
    {
        #region Fields
        private readonly ITextTemplatingEngineHost _textTemplatingEngineHost;
        private readonly CompilerErrorCollection _errors;
        private bool _hasLoggedErrors;
        #endregion

        #region Properties
        public bool HasLoggedErrors
        {
            get
            {
                if (!this._hasLoggedErrors && this._errors.HasErrors)
                {
                    this.ReportErrors();
                    this._hasLoggedErrors = true;
                }
                return this._errors.HasErrors;
            }
        }
        #endregion

        #region Constructor
        public TextTemplatingEngineLogger(ITextTemplatingEngineHost textTemplatingEngineHost)
        {
            this._textTemplatingEngineHost = textTemplatingEngineHost;
            this._errors = new CompilerErrorCollection();
        }
        #endregion

        #region ILogger Members
        public void LogMessage(string text) => Console.WriteLine(text);

        public void LogError(string code, string text, string source, int? line, int? column) => this.LogError(subCategory: null, code: code, text: text, source: source, line: line, column: column);
        public void LogError(string subCategory, string code, string text, string source, int? line, int? column)
        {
            // Apparently errors are reported with distinct description, even though a different position is supplied
            // To make it work we append the position to the message
            string normalizedText = String.Concat(text, ZeroWidthUtility.MaskText($" ({line},{column})"));
            this._errors.Add(new CompilerError(source, line ?? default, column ?? default, code, normalizedText));
        }
        #endregion

        #region Private Methods
        private void ReportErrors() => this._textTemplatingEngineHost.LogErrors(this._errors);
        #endregion
    }
}