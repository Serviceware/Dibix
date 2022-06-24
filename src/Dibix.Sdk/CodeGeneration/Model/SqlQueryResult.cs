using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public class SqlQueryResult
    {
	    public SqlQueryResultMode ResultMode { get; set; }
        public Token<string> Name { get; set; }
        public IList<TypeReference> Types { get; }
        public ICollection<string> Columns { get; }
        public string Converter { get; set; }
        public string SplitOn { get; set; }
        public TypeReference ProjectToType { get; set; }
        public TypeReference ReturnType { get; set; }

        public SqlQueryResult()
        {
            this.Types = new Collection<TypeReference>();
            this.Columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}