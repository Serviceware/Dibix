using System;
using System.Collections.Generic;
using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    internal sealed class CodeGenerationException : Exception
    {
        public CodeGenerationException(IEnumerable<Error> errors) : base($@"One or more errors occured during code generation:
{String.Join(Environment.NewLine, errors)}") { }
    }
}