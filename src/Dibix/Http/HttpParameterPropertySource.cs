namespace Dibix.Http
{
    public sealed class HttpParameterPropertySource : HttpParameterSource
    {
        public string SourceName { get; }
        public string PropertyName { get; }

        internal HttpParameterPropertySource(string sourceName, string propertyName)
        {
            this.SourceName = sourceName;
            this.PropertyName = propertyName;
        }
    }
}