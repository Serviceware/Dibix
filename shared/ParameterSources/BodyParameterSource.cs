namespace Dibix
{
    [ActionParameterSource("BODY")]
    internal sealed class BodyParameterSource : ActionParameterSourceDefinition<BodyParameterSource>
    {
        public const string RawPropertyName = "$RAW";
    }
}