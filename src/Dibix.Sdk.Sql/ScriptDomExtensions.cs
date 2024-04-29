using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Dibix.Sdk.Abstractions;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    public static class ScriptDomExtensions
    {
        public static void LogError(this ILogger logger, string text, TSqlFragment fragment, string source) => logger.LogMessage(LogCategory.Error, subCategory: null, code: null, text, source, fragment.StartLine, fragment.StartColumn);

        public static string Dump(this TSqlFragment fragment)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = fragment.GetFirstTokenIndex(); i <= fragment.LastTokenIndex; i++)
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

        public static BooleanExpression SkipParenthesis(this BooleanExpression booleanExpression)
        {
            BooleanExpression current = booleanExpression;
            
            while (current is BooleanParenthesisExpression booleanParenthesisExpression) 
                current = booleanParenthesisExpression.Expression;

            return current;
        }

        public static ScalarExpression SkipParenthesis(this ScalarExpression scalarExpression)
        {
            ScalarExpression current = scalarExpression;
            
            while (current is ParenthesisExpression parenthesisExpression) 
                current = parenthesisExpression.Expression;

            return current;
        }

        public static IEnumerable<TSqlParserToken> AsEnumerable(this TSqlFragment fragment) => AsEnumerable(fragment, fragment.GetFirstTokenIndex());
        public static IEnumerable<TSqlParserToken> AsEnumerable(this TSqlFragment fragment, int startIndex)
        {
            for (int i = startIndex; i <= fragment.LastTokenIndex; i++)
                yield return fragment.ScriptTokenStream[i];
        }

        // For 'END CONVERSATION @conversationhandle' the start index of the variable is returned, which is wrong and sounds like a bug in ScriptDom
        public static int GetFirstTokenIndex(this TSqlFragment fragment)
        {
            if (fragment is EndConversationStatement && fragment.ScriptTokenStream[fragment.FirstTokenIndex].TokenType != TSqlTokenType.End)
            {
                for (int i = fragment.FirstTokenIndex; i >= 0; i--)
                {
                    if (fragment.ScriptTokenStream[i].TokenType == TSqlTokenType.End)
                        return i;
                }
                throw new InvalidOperationException("Could not determine correct FirstTokenIndex of EndConversationStatement");
            }
            return fragment.FirstTokenIndex;
        }

        public static Identifier GetName(this ColumnReferenceExpression column) => column.MultiPartIdentifier?.Identifiers.Last();

        public static string ToFullName(this MultiPartIdentifier multiPartIdentifier) => String.Join(".", multiPartIdentifier.Identifiers.Select(x => $"[{x.Value}]"));

        public static ObjectIdentifier ToObjectIdentifier(this SchemaObjectName name)
        {
            IList<string> externalParts = new Collection<string>();
            IList<string> parts = new Collection<string>();

            if (name.ServerIdentifier != null)
                externalParts.Add(name.ServerIdentifier.Value);

            if (name.DatabaseIdentifier != null)
                externalParts.Add(name.DatabaseIdentifier.Value);

            if (name.SchemaIdentifier != null)
                parts.Add(name.SchemaIdentifier.Value);

            if (name.BaseIdentifier != null)
                parts.Add(name.BaseIdentifier.Value);

            ObjectIdentifier identifier = new ObjectIdentifier(externalParts, parts);
            return identifier;
        }

        public static QuerySpecification FindQuerySpecification(this QueryExpression expression)
        {
            if (expression is QuerySpecification query)
                return query;

            UnionStatementVisitor visitor = new UnionStatementVisitor();
            expression.Accept(visitor);
            return visitor.FirstQueryExpression;
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

        public static bool? IsNullable(this TSqlFragment sqlFragment, TSqlObject modelElement)
        {
            if (modelElement == null)
            {
                TSqlFragment target = sqlFragment;
                if (target is SelectScalarExpression scalarExpression)
                    target = scalarExpression.Expression;

                if (target is CastCall)
                    return true;

                return null;
            }

            ModelPropertyClass nullableProperty = GetNullableProperty(modelElement.ObjectType);
            if (nullableProperty == null)
                return null; // Not supported

            return modelElement.GetProperty<bool>(nullableProperty);
        }

        public static SqlDataType GetDataType(this TSqlFragment sqlFragment, TSqlObject modelElement)
        {
            if (modelElement == null)
            {
                TSqlFragment target = sqlFragment;
                if (target is SelectScalarExpression scalarExpression)
                    target = scalarExpression.Expression;

                if (target is CastCall cast && cast.DataType is SqlDataTypeReference sqlDataTypeReference)
                    return (SqlDataType)sqlDataTypeReference.SqlDataTypeOption;

                if (target is Literal literal)
                    return ToDataType(literal);

                return SqlDataType.Unknown;
            }

            // DataType property not available
            TSqlObject parent = modelElement.GetParent(DacQueryScopes.All);
            if (parent?.ObjectType == ModelSchema.TableValuedFunction
             || parent?.ObjectType == ModelSchema.View)
                return SqlDataType.Unknown;

            ModelRelationshipClass dataTypeRelationship = GetDataTypeRelationship(modelElement.ObjectType);
            TSqlObject dataType = modelElement.GetReferenced(dataTypeRelationship, DacQueryScopes.All).Single();
            return dataType.GetProperty<SqlDataType>(DataType.SqlDataType);
        }

        private static SqlDataType ToDataType(Literal literal)
        {
            bool isNationalString = literal is StringLiteral stringLiteral && stringLiteral.IsNational;
            return ToDataType(literal.LiteralType, isNationalString);
        }

        private static SqlDataType ToDataType(LiteralType literalType, bool isNationalString)
        {
            switch (literalType)
            {
                case LiteralType.Integer: return SqlDataType.Int;
                case LiteralType.Real: return SqlDataType.Real;
                case LiteralType.Money: return SqlDataType.Money;
                case LiteralType.Binary: return SqlDataType.Binary;
                
                case LiteralType.String when isNationalString: return SqlDataType.NVarChar;
                case LiteralType.String: return SqlDataType.VarChar;

                case LiteralType.Null: 
                case LiteralType.Default:
                case LiteralType.Max:
                case LiteralType.Odbc:
                case LiteralType.Identifier:
                case LiteralType.Numeric:
                    return SqlDataType.Unknown;

                default:
                    throw new ArgumentOutOfRangeException(nameof(literalType), literalType, null);
            }
        }

        private static ModelRelationshipClass GetDataTypeRelationship(ModelTypeClass type)
        {
            ModelRelationshipClass relationship;

            if (type == ModelSchema.Column)
                relationship = Microsoft.SqlServer.Dac.Model.Column.DataType;
            else if (type == ModelSchema.Parameter)
                relationship = Parameter.DataType;
            else if (type == ModelSchema.ScalarFunction)
                relationship = ScalarFunction.ReturnType;
            else
                throw new ArgumentOutOfRangeException(nameof(type), $"{type}.{type.Name}", null);

            return relationship;
        }

        private static ModelPropertyClass GetNullableProperty(ModelTypeClass type)
        {
            ModelPropertyClass property;

            if (type == ModelSchema.Column)
                property = Microsoft.SqlServer.Dac.Model.Column.Nullable;
            else if (type == ModelSchema.Parameter)
                property = Parameter.IsNullable;
            else if (type == ModelSchema.ScalarFunction)
                return null;
            else
                throw new ArgumentOutOfRangeException(nameof(type), type, null);

            return property;
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