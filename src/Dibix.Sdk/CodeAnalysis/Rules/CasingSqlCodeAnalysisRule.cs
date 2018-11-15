using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class CasingSqlCodeAnalysisRule : SqlCodeAnalysisRule<CasingSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 1;
        public override string ErrorMessage => "Invalid casing for '{0}' [{1}]";
    }

    public sealed class CasingSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
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

        public override void Visit(TSqlParserToken token)
        {
            if (!TokenWhiteList.Contains(token.TokenType))
                this.Visit(token, token.TokenType);
        }

        public override void Visit(FunctionCall node)
        {
            if (SqlConstants.ReservedFunctionNames.Contains(node.FunctionName.Value))
                return;

            node.FunctionName.Visit(x => this.Visit(x, TSqlTokenType.Identifier));
        }

        public override void Visit(SqlDataTypeReference node)
        {
            node.Visit(x => this.Visit(x, TSqlTokenType.Identifier));
        }

        public override void Visit(PredicateSetStatement node)
        {
            node.Visit(x => this.Visit(x, TSqlTokenType.Identifier));
        }

        private void Visit(TSqlParserToken token, TSqlTokenType expectedTokenType)
        {
            if (token.TokenType != expectedTokenType)
                return;

            if (!token.Text.All(x => !Char.IsLetter(x) || Char.IsUpper(x)))
                base.Fail(token, token.Text, token.TokenType);
        }
    }
}