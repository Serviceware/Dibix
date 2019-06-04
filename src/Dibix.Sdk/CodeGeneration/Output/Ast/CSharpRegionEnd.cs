namespace Dibix.Sdk.CodeGeneration
{
    internal class CSharpRegionEnd : CSharpStatement
    {
        public override void Write(StringWriter writer) => writer.Write("#endregion");
    }
}