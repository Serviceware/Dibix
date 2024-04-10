namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterBodySource : ActionParameterSource
    {
        public Token<string> ConverterName { get; }

        public ActionParameterBodySource(Token<string> converterName) => ConverterName = converterName;
    }
}