namespace Dibix.Http.Server
{
    public sealed class HttpParameterBodySource : HttpParameterSource
    {
        public string ConverterName { get; }
        public override string Description => "BODY";

        internal HttpParameterBodySource(string converterName)
        {
            this.ConverterName = converterName;
        }
    }
}