namespace Dibix.Sdk.CodeGeneration.CSharp
{
    internal class CSharpRegionEnd : CSharpExpression
    {
        public override void Write(StringWriter writer) => writer.Write("#endregion");
    }
}