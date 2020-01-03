﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SqlStatementInfo 
    {
        public string Source { get; set; }
        public Namespace Namespace { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public string ProcedureName { get; set; }
        public bool MergeGridResult { get; set; }
        public bool IsFileApi { get; set; }
        public bool GenerateInputClass { get; set; }
        public bool Async { get; set; }
        public CommandType? CommandType { get; set; }
        public ContractName ResultType { get; set; }
        public GridResultType GridResultType { get; set; }
        public IList<SqlQueryParameter> Parameters { get; }
        public IList<SqlQueryResult> Results { get; }

        public SqlStatementInfo() 
        {
            this.Parameters = new Collection<SqlQueryParameter>();
            this.Results = new Collection<SqlQueryResult>();
        }
    }
}