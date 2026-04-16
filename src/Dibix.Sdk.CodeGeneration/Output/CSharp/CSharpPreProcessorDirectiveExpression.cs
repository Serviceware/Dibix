namespace Dibix.Sdk.CodeGeneration.CSharp
{
    public class CSharpPreprocessorDirectiveExpression : CSharpExpression
    {
        private readonly string _value;
        private readonly bool _isBegin;

        internal CSharpPreprocessorDirectiveExpression(string value, bool isBegin)
        {
            _value = value;
            _isBegin = isBegin;
        }

        public override void Write(StringWriter writer)
        {
            if (!_isBegin)
                writer.WriteLine();

            writer.WriteRaw($"#{_value}");

            if (_isBegin)
                writer.WriteLine();
        }
    }
}