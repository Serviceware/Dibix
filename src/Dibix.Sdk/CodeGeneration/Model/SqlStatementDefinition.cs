using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SqlStatementDefinition : SchemaDefinition
    {
        public FormattedSqlStatement Statement { get; set; }
        public string ProcedureName { get; set; }
        public bool MergeGridResult { get; set; }
        public bool GenerateResultClass { get; set; }
        public bool GenerateInputClass { get; set; }
        public bool Async { get; set; }
        public ISqlElement FileResult { get; set; }
        public TypeReference ResultType { get; set; }
        public IList<SqlQueryParameter> Parameters { get; }
        public IList<SqlQueryResult> Results { get; }
        public ICollection<ErrorResponse> ErrorResponses { get; }

        public SqlStatementDefinition(string @namespace, string definitionName, SchemaDefinitionSource source) : base(@namespace, definitionName, source)
        {
            this.Parameters = new Collection<SqlQueryParameter>();
            this.Results = new Collection<SqlQueryResult>();
            this.ErrorResponses = new Collection<ErrorResponse>();
        }
    }
}