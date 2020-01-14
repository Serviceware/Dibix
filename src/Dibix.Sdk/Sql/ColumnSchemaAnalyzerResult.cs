using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.SqlServer.Dac.Extensibility;

namespace Dibix.Sdk.Sql
{
    internal sealed class ColumnSchemaAnalyzerResult
    {
        public ICollection<ExtensibilityError> Errors { get; }
        public ICollection<ColumnElementHit> Hits { get; }

        public ColumnSchemaAnalyzerResult()
        {
            this.Errors = new Collection<ExtensibilityError>();
            this.Hits = new Collection<ColumnElementHit>();
        }
    }
}