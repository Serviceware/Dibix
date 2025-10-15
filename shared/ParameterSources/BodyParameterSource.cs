namespace Dibix
{
    [ActionParameterSource("BODY")]
    internal sealed class BodyParameterSource : ActionParameterSourceDefinition<BodyParameterSource>
    {
        public const string RawPropertyName = "$RAW";
        public const string MediaTypePropertyName = "$MEDIATYPE";
        public const string FileNamePropertyName = "$FILENAME";
        public const string LengthPropertyName = "$LENGTH";
    }
}