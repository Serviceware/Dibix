using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SqlParserVisitor : TSqlFragmentVisitor
    {
        internal string ProductName { get; set; }
        internal string AreaName { get; set; }
        internal TSqlElementLocator ElementLocator { get; set; }
        internal ISqlStatementFormatter Formatter { get; set; }
        internal IContractResolverFacade ContractResolver { get; set; }
        internal IErrorReporter ErrorReporter { get; set; }
        internal SqlStatementInfo Target { get; set; }
        internal ICollection<SqlHint> Hints { get; }

        protected SqlParserVisitor()
        {
            this.Hints = new Collection<SqlHint>();
        }
    }
}