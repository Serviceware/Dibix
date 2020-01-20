using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.SqlServer.Dac.Extensibility;

namespace Dibix.Sdk.Sql
{
    public sealed class SchemaAnalyzerResult
    {
        public ICollection<ExtensibilityError> Errors { get; }
        public ICollection<ElementLocation> Locations { get; }

        public SchemaAnalyzerResult()
        {
            this.Errors = new Collection<ExtensibilityError>();
            this.Locations = new Collection<ElementLocation>();
        }
    }
}