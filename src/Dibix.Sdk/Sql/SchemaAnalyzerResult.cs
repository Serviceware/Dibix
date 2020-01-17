using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.SqlServer.Dac.Extensibility;

namespace Dibix.Sdk.Sql
{
    internal sealed class SchemaAnalyzerResult
    {
        public ICollection<ExtensibilityError> Errors { get; }
        public ICollection<ElementDescriptor> Hits { get; }

        public SchemaAnalyzerResult()
        {
            this.Errors = new Collection<ExtensibilityError>();
            this.Hits = new Collection<ElementDescriptor>();
        }
    }
}