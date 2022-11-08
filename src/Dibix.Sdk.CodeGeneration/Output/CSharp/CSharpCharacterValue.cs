namespace Dibix.Sdk.CodeGeneration.CSharp
{
    internal sealed class CSharpCharacterValue : CSharpValue
    {
        public CSharpCharacterValue(char value) : base(value.ToString()) { }

        public override void Write(StringWriter writer)
        {
            writer.WriteRaw('\'');

            base.Write(writer);

            writer.WriteRaw('\'');
        }
    }
}