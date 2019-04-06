namespace Dibix.Sdk.CodeGeneration.Ast
{
    internal class CSharpRegionEnd : CSharpStatement
    {
        public override void Write(StringWriter writer) => writer.Write("#endregion");
    }
}