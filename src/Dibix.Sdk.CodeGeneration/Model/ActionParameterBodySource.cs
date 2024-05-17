namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterBodySource : ActionParameterSource
    {
        public Token<string> ConverterName { get; }
        public override TypeReference Type => null;

        public ActionParameterBodySource(Token<string> converterName) => ConverterName = converterName;
    }
}