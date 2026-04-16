using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class PreprocessorDirective
    {
        public static CSharpPreprocessorDirectiveExpression IfNetFrameworkBegin => new CSharpPreprocessorDirectiveExpression("if NETFRAMEWORK", isBegin: true);
        public static CSharpPreprocessorDirectiveExpression IfNetFrameworkEnd => new CSharpPreprocessorDirectiveExpression("endif", isBegin: false);
        public static CSharpPreprocessorDirective IfNetFramework => new CSharpPreprocessorDirective(IfNetFrameworkBegin, IfNetFrameworkEnd);
    }
}