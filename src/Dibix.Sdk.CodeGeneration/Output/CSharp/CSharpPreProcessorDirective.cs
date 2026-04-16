namespace Dibix.Sdk.CodeGeneration.CSharp
{
    public class CSharpPreprocessorDirective : CSharpStatementScope
    {
        private readonly CSharpPreprocessorDirectiveExpression _begin;
        private readonly CSharpPreprocessorDirectiveExpression _end;

        internal CSharpPreprocessorDirective(CSharpPreprocessorDirectiveExpression begin, CSharpPreprocessorDirectiveExpression end)
        {
            _begin = begin;
            _end = end;
        }

        public override void Write(StringWriter writer)
        {
            _begin.Write(writer);

            base.Write(writer);

            _end.Write(writer);
        }
    }
}