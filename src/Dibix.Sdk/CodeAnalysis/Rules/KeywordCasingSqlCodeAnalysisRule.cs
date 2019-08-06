using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class KeywordCasingSqlCodeAnalysisRule : SqlCodeAnalysisRule<KeywordCasingSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 1;
        public override string ErrorMessage => "Invalid casing for '{0}' [{1}]";
    }

    public sealed class KeywordCasingSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        private static readonly HashSet<TSqlTokenType> TokenWhiteList = new HashSet<TSqlTokenType>
        {
            TSqlTokenType.AsciiStringLiteral,
            TSqlTokenType.Bang,
            TSqlTokenType.Comma,
            TSqlTokenType.Dot,
            TSqlTokenType.EndOfFile,
            TSqlTokenType.EqualsSign,
            TSqlTokenType.GreaterThan,
            TSqlTokenType.HexLiteral,
            TSqlTokenType.Identifier,
            TSqlTokenType.Integer,
            TSqlTokenType.LeftCurly,
            TSqlTokenType.LeftParenthesis,
            TSqlTokenType.LessThan,
            TSqlTokenType.Minus,
            TSqlTokenType.MultilineComment,
            TSqlTokenType.Plus,
            TSqlTokenType.PseudoColumn,
            TSqlTokenType.QuotedIdentifier,
            TSqlTokenType.RightCurly,
            TSqlTokenType.RightParenthesis,
            TSqlTokenType.Semicolon,
            TSqlTokenType.SingleLineComment,
            TSqlTokenType.Star,
            TSqlTokenType.UnicodeStringLiteral,
            TSqlTokenType.Variable,
            TSqlTokenType.WhiteSpace
        };
        private IdentifierVisitor _identifierVisitor;

        public override void Visit(TSqlStatement node)
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
                    .Where(x => x.TokenType == TSqlTokenType.Identifier)
                    .Each(this._tokenVisitor);
            }

            // SYSNAME
            public override void Visit(UserDataTypeReference node)
            {
                if (String.Compare(node.Name.BaseIdentifier.Value, "SYSNAME", StringComparison.OrdinalIgnoreCase) == 0 && node.Name.SchemaIdentifier == null)
                    this.VisitFirstToken(node);
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
                    .Where(x => x.TokenType == TSqlTokenType.Identifier)
                    .Each(this._tokenVisitor);
            }

            private void VisitFirstToken(TSqlFragment fragment)
            {
                this._tokenVisitor(fragment.ScriptTokenStream[fragment.FirstTokenIndex]);
            }
        }
    }
}