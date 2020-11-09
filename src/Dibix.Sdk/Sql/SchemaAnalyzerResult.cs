using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.SqlServer.Dac.Extensibility;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    public sealed class SchemaAnalyzerResult
    {
        public ICollection<ExtensibilityError> Errors { get; }
        public ICollection<TSqlFragment> DDLStatements { get; }
        public IDictionary<int, ElementLocation> Locations { get; }

        public SchemaAnalyzerResult()
        {
            this.Errors = new Collection<ExtensibilityError>();
            this.DDLStatements = new Collection<TSqlFragment>();
            this.Locations = new Dictionary<int, ElementLocation>();
        }
    }
}