using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace Dibix.Sdk.CodeGeneration.CSharp
{
    internal static class CSharpKeywords
    {
        private static readonly string[] ReservedKeywords = Enum.GetValues(typeof(SyntaxKind))
                                                                .Cast<SyntaxKind>()
                                                                .Select(SyntaxFacts.GetText)
                                                                .Where(x => x.Length > 0 && x.All(Char.IsLower))
                                                                .OrderBy(x => x)
                                                                .ToArray();

        public static bool IsReservedKeyword(string text) => ReservedKeywords.Contains(text);
    }
}