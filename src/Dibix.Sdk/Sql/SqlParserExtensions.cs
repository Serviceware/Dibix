﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
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

        public static string NormalizeBooleanExpression(this string expression) => BooleanExpressionNormalizer.Normalize(expression);
        public static string Normalize(this TSqlFragment fragment)
        {
            IdentifierVisitor visitor = new IdentifierVisitor();
            fragment.Accept(visitor);
            return ScriptDomFacade.Generate(fragment);
        }

        public static IEnumerable<TSqlParserToken> AsEnumerable(this TSqlFragment fragment) => AsEnumerable(fragment, fragment.FirstTokenIndex);
        public static IEnumerable<TSqlParserToken> AsEnumerable(this TSqlFragment fragment, int startIndex)
        {
            for (int i = startIndex; i <= fragment.LastTokenIndex; i++)
                yield return fragment.ScriptTokenStream[i];
        }

        public static Identifier GetName(this ColumnReferenceExpression column) => column.MultiPartIdentifier?.Identifiers.Last();

        public static QuerySpecification FindQuerySpecification(this QueryExpression expression)
        {
            if (expression is QuerySpecification query)
                return query;

            UnionStatementVisitor visitor = new UnionStatementVisitor();
            expression.Accept(visitor);
            return visitor.FirstQueryExpression;
        }

        public static bool ContainsIf(this TSqlFragment fragment)
        {
            IfStatementVisitor visitor = new IfStatementVisitor();
            fragment.Accept(visitor);
            return visitor.Found;
        }

        public static IEnumerable<TableReference> Recursive(this IEnumerable<TableReference> references)
        {
            foreach (TableReference tableReference in references)
            {
                if (tableReference is QualifiedJoin join)
                {
                    yield return join.FirstTableReference;
                    yield return join.SecondTableReference;
                }
                else
                    yield return tableReference;
            }
        }

        public static bool IsTemporaryTable(this CreateTableStatement node) => node.SchemaObjectName.BaseIdentifier.Value[0] == '#';

        public static TSqlFragment FindChild(this TSqlFragment sqlFragment, SourceInformation source)
        {
            TSqlFragment child = FragmentChildVisitor.FindChild(sqlFragment, source.StartLine, source.StartColumn);
            if (child == null)
            {
                TSqlFragment externalSqlFragment = ScriptDomFacade.Load(source.SourceName);
                child = FragmentChildVisitor.FindChild(externalSqlFragment, source.StartLine, source.StartColumn);
            }

            if (child == null)
                throw new InvalidOperationException($"Could not find child fragment at ({source.StartLine},{source.StartColumn})");

            return child;
        }


        private sealed class BooleanExpressionNormalizer : TSqlFragmentVisitor
        {
            private BooleanExpression Match { get; set; }

            public override void Visit(BooleanExpression node)
            {
                if (this.Match != null)
                    return;

                this.Match = node;
            }

            public static string Normalize(string expression)
            {
                if (expression == null)
                    return null;

                TSqlFragment wrapper = ScriptDomFacade.Parse($"SELECT IIF({expression}, 1, 0)");
                BooleanExpressionNormalizer visitor = new BooleanExpressionNormalizer();
                wrapper.Accept(visitor);
                return visitor.Match?.Normalize();
            }
        }

        private sealed class IdentifierVisitor : TSqlFragmentVisitor
        {
            public override void Visit(Identifier node)
            {
                if (node.QuoteType == QuoteType.NotQuoted)
                    node.QuoteType = QuoteType.SquareBracket;
            }
        }

        private sealed class IfStatementVisitor : TSqlFragmentVisitor
        {
            public bool Found { get; private set; }

            public override void ExplicitVisit(IfStatement node) => this.Found = true;
        }

        private sealed class UnionStatementVisitor : TSqlFragmentVisitor
        {
            public QuerySpecification FirstQueryExpression { get; private set; }

            public override void Visit(QuerySpecification node)
            {
                if (this.FirstQueryExpression != null)
                    return;

                this.FirstQueryExpression = node;
            }
        }

        private sealed class FragmentChildVisitor : TSqlFragmentVisitor
        {
            private readonly int _line;
            private readonly int _column;

            private TSqlFragment Match { get; set; }

            private FragmentChildVisitor(int line, int column)
            {
                this._line = line;
                this._column = column;
            }

            public override void Visit(TSqlFragment node)
            {
                if (node.StartLine != this._line || node.StartColumn != this._column)
                    return;
                
                this.Match = node;
            }

            public static TSqlFragment FindChild(TSqlFragment node, int line, int column)
            {
                FragmentChildVisitor visitor = new FragmentChildVisitor(line, column);
                node.Accept(visitor);
                return visitor.Match;
            }
        }
    }
}
