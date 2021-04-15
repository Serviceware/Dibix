namespace Dibix.Http.Server
{
    public sealed class HttpParameterBodySource : HttpParameterSource
    {
        public string ConverterName { get; }

        internal HttpParameterBodySource(string converterName)
        {
            this.ConverterName = converterName;
        }
    }
}