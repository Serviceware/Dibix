using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionParameterPropertySource : ActionParameterSource
    {
        public string SourceName { get; }
        public string PropertyName { get; }
        public string Converter { get; set; }
        public IDictionary<string, ActionParameterSource> ItemSources { get; }

        internal ActionParameterPropertySource(string sourceName, string propertyName)
        {
            this.SourceName = sourceName;
            this.PropertyName = propertyName;
            this.ItemSources = new ConcurrentDictionary<string, ActionParameterSource>();
        }
    }
}