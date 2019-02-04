using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;

namespace Dibix.Sdk.CodeGeneration
{
    public class SqlStatementInfo 
    {
        public string SourcePath { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public string ProcedureName { get; set; }
        public CommandType? CommandType { get; set; }
        public string ResultTypeName { get; set; }
        public bool IsAggregateResult { get; set; }
        public IList<SqlQueryParameter> Parameters { get; }
        public IList<SqlQueryResult> Results { get; }

        public SqlStatementInfo() 
        {
            this.Parameters = new Collection<SqlQueryParameter>();
            this.Results = new Collection<SqlQueryResult>();
        }
    }
}