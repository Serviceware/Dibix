using System;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Dac.CodeAnalysis.Rules
{
    internal static class SqlParserExtensions
    {
        public static void Visit(this TSqlFragment fragment, Action<TSqlParserToken> visitor)
        {
            if (fragment.ScriptTokenStream == null)
                return;

            int startIndex = fragment.FirstTokenIndex < 0 ? 0 : fragment.FirstTokenIndex;
            int endIndex = fragment.LastTokenIndex < 0 ? fragment.ScriptTokenStream.Count - 1 : fragment.LastTokenIndex;
            for (int i = startIndex; i <= endIndex; i++)
                visitor(fragment.ScriptTokenStream[i]);
        }
    }
}