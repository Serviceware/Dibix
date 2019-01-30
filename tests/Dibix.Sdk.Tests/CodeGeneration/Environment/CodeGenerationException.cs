using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    internal sealed class CodeGenerationException : Exception
    {
        public CodeGenerationException(IEnumerable<CompilerError> errors) : base($@"One or more errors occured during code generation:
{String.Join(Environment.NewLine, errors)}") { }
    }
}