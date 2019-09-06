namespace Dibix.Sdk.CodeGeneration.CSharp
{
    internal class CSharpRegionEnd : CSharpStatement
    {
        public override void Write(StringWriter writer) => writer.Write("#endregion");
    }
}