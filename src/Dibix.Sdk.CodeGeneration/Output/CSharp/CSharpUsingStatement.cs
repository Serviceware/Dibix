namespace Dibix.Sdk.CodeGeneration.CSharp
{
    internal class CSharpUsingStatement : CSharpExpression
    {
        private readonly string _using;

        public CSharpUsingStatement(string @using)
        {
            _using = @using;
        }

        public override void Write(StringWriter writer)
        {
            writer.Write($"using {_using};");
        }
    }
}