using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public class SqlQueryResult
    {
	    public SqlQueryResultMode ResultMode { get; set; }
        public string Name { get; set; }
        public IList<ContractInfo> Contracts { get; }
        public IList<string> Columns { get; }
        public string Converter { get; set; }
        public string SplitOn { get; set; }
        public string ResultTypeName { get; set; }

        public SqlQueryResult()
        {
            this.Contracts = new Collection<ContractInfo>();
            this.Columns = new Collection<string>();
        }
    }
}