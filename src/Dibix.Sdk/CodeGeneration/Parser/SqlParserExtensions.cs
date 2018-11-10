using System;
using System.Text;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class SqlParserExtensions
    {
        public static string Dump(this TSqlFragment fragment)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = fragment.FirstTokenIndex; i <= fragment.LastTokenIndex; i++)
                sb.Append(fragment.ScriptTokenStream[i].Text);

            return sb.ToString();
        }

        public static void Visit(this TSqlFragment fragment, Action<TSqlParserToken> visitor)
        {
            for (int i = fragment.FirstTokenIndex; i <= fragment.LastTokenIndex; i++)
                visitor(fragment.ScriptTokenStream[i]);
        }

        public static bool ContainsIf(this TSqlFragment fragment)
        {
            IfStatementVisitor visitor = new IfStatementVisitor();
            fragment.Accept(visitor);
            return visitor.Found;
        }

        private class IfStatementVisitor : TSqlFragmentVisitor
        {
            public bool Found { get; private set; }

            public override void ExplicitVisit(IfStatement node)
            {
                this.Found = true;
            }
        }
    }
}
