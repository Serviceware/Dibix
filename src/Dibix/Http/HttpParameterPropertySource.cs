using System.Collections.Generic;

namespace Dibix.Http
{
    public sealed class HttpParameterPropertySource : HttpParameterSource
    {
        public string SourceName { get; }
        public string PropertyName { get; }
        public IDictionary<string, HttpParameterSource> ItemSources { get; }

        internal HttpParameterPropertySource(string sourceName, string propertyName)
        {
            this.SourceName = sourceName;
            this.PropertyName = propertyName;
            this.ItemSources = new Dictionary<string, HttpParameterSource>();
        }
    }
}