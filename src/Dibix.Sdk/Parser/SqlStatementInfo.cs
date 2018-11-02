using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;

namespace Dibix.Sdk
{
    public class SqlStatementInfo 
    {
        public string SourcePath { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public string ProcedureName { get; set; }
        public CommandType? CommandType { get; set; }
        public string ResultTypeName { get; set; }
        public IList<SqlQueryParameter> Parameters { get; private set; }
        public IList<SqlQueryResult> Results { get; private set; }

        public SqlStatementInfo() 
        {
            this.Parameters = new Collection<SqlQueryParameter>();
            this.Results = new Collection<SqlQueryResult>();
        }
    }
}