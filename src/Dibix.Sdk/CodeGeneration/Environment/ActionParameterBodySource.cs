namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionParameterBodySource : ActionParameterSource
    {
        public string ConverterName { get; }

        internal ActionParameterBodySource(string converterName) => this.ConverterName = converterName;
    }
}