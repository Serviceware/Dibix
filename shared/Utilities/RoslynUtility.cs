using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Dibix
{
    internal static class RoslynUtility
    {
        public static CSharpCompilation AddReference<T>(this CSharpCompilation compilation) => compilation.AddReferences(MetadataReferenceFactory.FromType<T>());

        public static void VerifyCompilation(Compilation compilation) => VerifyCompilation(compilation.GetDiagnostics());
        public static void VerifyCompilation(GeneratorRunResult result)
        {
            if (result.Exception != null)
                throw result.Exception;

            VerifyCompilation(result.Diagnostics);
        }
        public static void VerifyCompilation(ImmutableArray<Diagnostic> diagnostics)
        {
            ICollection<Diagnostic> errors = diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error).ToArray();
            if (!errors.Any())
                return;

            string errorsText = String.Join(Environment.NewLine, errors.Select(x => x));
            throw new CodeCompilationException(errorsText);
        }
    }

    internal sealed class CodeCompilationException : Exception
    {
        public CodeCompilationException(string errorMessages) : base($@"One or more errors occured while validating the generated code:
{errorMessages.TrimEnd(Environment.NewLine.ToCharArray())}")
        {
        }
    }
}