﻿using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk
{
    public class SqlQueryResult
    {
	    public SqlQueryResultMode ResultMode { get; set; }
        public string Name { get; set; }
        public IList<TypeInfo> Types { get; private set; }
        public IList<string> Columns { get; set; }
        public string Converter { get; set; }
        public string SplitOn { get; set; }

        public SqlQueryResult()
        {
            this.Types = new Collection<TypeInfo>();
            this.Columns = new Collection<string>();
        }
    }
}