using System.Collections.Generic;

namespace Dibix.Http
{
    public sealed class HttpParameterPropertySource : HttpParameterSource
    {
        public string SourceName { get; }
        public string PropertyName { get; }
        public string ConverterName { get; }
        public IDictionary<string, HttpParameterSource> ItemSources { get; }

        internal HttpParameterPropertySource(string sourceName, string propertyName, string converterName)
        {
            this.SourceName = sourceName;
            this.PropertyName = propertyName;
            this.ConverterName = converterName;
            this.ItemSources = new Dictionary<string, HttpParameterSource>();
        }
    }
}