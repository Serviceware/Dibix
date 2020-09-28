using System.Collections.Generic;

namespace Dibix.Http
{
    public sealed class HttpParameterPropertySource : HttpParameterSource
    {
        public string SourceName { get; }
        public string PropertyPath { get; }
        public string ConverterName { get; }
        public IDictionary<string, HttpParameterSource> ItemSources { get; }

        internal HttpParameterPropertySource(string sourceName, string propertyPath, string converterName)
        {
            this.SourceName = sourceName;
            this.PropertyPath = propertyPath;
            this.ConverterName = converterName;
            this.ItemSources = new Dictionary<string, HttpParameterSource>();
        }
    }
}