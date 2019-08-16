using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    internal static class SqlModelExtensions
    {
        public static TSqlFragment GetScriptDom(this TSqlObject element) => TSqlObjectScriptDomAccessor.GetScriptDom(element);
    }
}
