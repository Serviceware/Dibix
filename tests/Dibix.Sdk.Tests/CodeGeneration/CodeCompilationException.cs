using System;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    internal sealed class CodeCompilationException : Exception
    {
        public CodeCompilationException(string errorMessages) : base($@"One or more errors occured while validating the generated code:
{errorMessages.TrimEnd(Environment.NewLine.ToCharArray())}") { }
    }
}