using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 1)]
    public sealed class KeywordCasingSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        private static readonly ICollection<TSqlTokenType> TokenWhiteList = new HashSet<TSqlTokenType>
        {
            SqlTokenType.AsciiStringLiteral,
            SqlTokenType.Bang,
            SqlTokenType.Comma,
            SqlTokenType.Dot,
            SqlTokenType.EndOfFile,
            SqlTokenType.EqualsSign,
            SqlTokenType.GreaterThan,
            SqlTokenType.HexLiteral,
            SqlTokenType.Identifier,
            SqlTokenType.Integer,
            SqlTokenType.LeftCurly,
            SqlTokenType.LeftParenthesis,
            SqlTokenType.LessThan,
            SqlTokenType.Minus,
            SqlTokenType.MultilineComment,
            SqlTokenType.Plus,
            SqlTokenType.PseudoColumn,
            SqlTokenType.QuotedIdentifier,
            SqlTokenType.RightCurly,
            SqlTokenType.RightParenthesis,
            SqlTokenType.Semicolon,
            SqlTokenType.SingleLineComment,
            SqlTokenType.Star,
            SqlTokenType.UnicodeStringLiteral,
            SqlTokenType.Variable,
            SqlTokenType.WhiteSpace
        };
        private IdentifierVisitor _identifierVisitor;

        protected override string ErrorMessageTemplate => "Invalid casing for '{0}' [{1}]";

        protected override void BeginStatement(TSqlScript node)
        {
            if (this._identifierVisitor != null)
                return;

            this._identifierVisitor = new IdentifierVisitor(this.Check);
            node.AcceptChildren(this._identifierVisitor);
        }

        protected override void Visit(TSqlParserToken token)
        {
            if (TokenWhiteList.Contains(token.TokenType))
                return;

            this.Check(token);
        }

        private void Check(TSqlParserToken token)
        {
            if (!token.Text.All(x => !Char.IsLetter(x) || Char.IsUpper(x)))
                base.Fail(token, token.Text, token.TokenType);
        }

        private class IdentifierVisitor : TSqlFragmentVisitor
        {
            private readonly Action<TSqlParserToken> _tokenVisitor;

            public IdentifierVisitor(Action<TSqlParserToken> tokenVisitor)
            {
                this._tokenVisitor = tokenVisitor;
            }

            public override void Visit(CastCall node) => this.VisitFirstToken(node);

            public override void Visit(FunctionCall node)
            {
                if (SqlConstants.ReservedFunctionNames.Contains(node.FunctionName.Value))
                    return;

                // Excluded user function calls like '[dbo].[any]()'
                if (node.FunctionName.QuoteType == QuoteType.SquareBracket || node.CallTarget is MultiPartIdentifierCallTarget)
                    return;

                this.VisitFirstToken(node.FunctionName);
            }

            public override void Visit(SqlDataTypeReference node)
            {
                node.AsEnumerable()
                    .Where(x => x.TokenType == SqlTokenType.Identifier)
                    .Each(this._tokenVisitor);
            }

            // PARTITION
            public override void Visit(OverClause node)
            {
                TSqlParserToken partition = node.AsEnumerable().FirstOrDefault(x => x.Text.ToUpperInvariant() == "PARTITION");

                if (partition != null)
                    this._tokenVisitor(partition);
            }

            public override void Visit(PredicateSetStatement node)
            {
                node.AsEnumerable()
                    .Where(x => x.TokenType == SqlTokenType.Identifier)
                    .Each(this._tokenVisitor);
            }

            private void VisitFirstToken(TSqlFragment fragment)
            {
                this._tokenVisitor(fragment.ScriptTokenStream[fragment.FirstTokenIndex]);
            }
        }
    }
}