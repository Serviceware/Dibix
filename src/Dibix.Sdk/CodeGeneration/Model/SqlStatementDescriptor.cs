using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SqlStatementDescriptor
    {
        public string Source { get; set; }
        public string Namespace { get; set; }
        public string Name { get; set; }
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

        public SqlStatementDescriptor() 
        {
            this.Parameters = new Collection<SqlQueryParameter>();
            this.Results = new Collection<SqlQueryResult>();
            this.ErrorResponses = new Collection<ErrorResponse>();
        }
    }
}