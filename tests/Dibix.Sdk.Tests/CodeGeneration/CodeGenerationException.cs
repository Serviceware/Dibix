using System;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    internal sealed class CodeGenerationException : Exception
    {
        public CodeGenerationException(string errorMessages) : base($@"One or more errors occured during code generation:
{errorMessages.TrimEnd(Environment.NewLine.ToCharArray())}") { }
    }
}