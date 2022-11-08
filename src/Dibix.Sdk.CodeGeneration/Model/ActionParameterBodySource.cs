namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterBodySource : ActionParameterSource
    {
        public string ConverterName { get; }

        public ActionParameterBodySource(string converterName) => this.ConverterName = converterName;
    }
}