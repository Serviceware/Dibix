using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public class SqlQueryResult
    {
	    public SqlQueryResultMode ResultMode { get; set; }
        public string Name { get; set; }
        public IList<TypeReference> Types { get; }
        public IList<string> Columns { get; }
        public string Converter { get; set; }
        public string SplitOn { get; set; }
        public TypeReference ProjectToType { get; set; }
        public TypeReference ResultType => this.ProjectToType ?? this.Types[0];

        public SqlQueryResult()
        {
            this.Types = new Collection<TypeReference>();
            this.Columns = new Collection<string>();
        }
    }
}