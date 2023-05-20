﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class EnumContractVisitor : TSqlFragmentVisitor
    {
        private readonly string _file;
        private readonly string _productName;
        private readonly string _areaName;
        private readonly ILogger _logger;
        private readonly IDictionary<Token<string>, EnumSchema> _definitionMap;

        public ICollection<EnumSchema> Definitions => _definitionMap.Values;

        public EnumContractVisitor(string file, string productName, string areaName, ILogger logger)
        {
            _file = file;
            _productName = productName;
            _areaName = areaName;
            _logger = logger;
            _definitionMap = new Dictionary<Token<string>, EnumSchema>();
        }

        public override void ExplicitVisit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            ISqlMarkupDeclaration markup = SqlMarkupReader.Read(node, SqlMarkupCommentKind.SingleLine, _file, _logger);
            if (!markup.TryGetSingleElement(SqlMarkupKey.Enum, _file, _logger, out ISqlElement enumElement))
                return;

            _ = enumElement.TryGetPropertyValue(SqlEnumMarkupProperty.Name, isDefault: true, out Token<string> enumContractName);
            _ = markup.TryGetSingleElementValue(SqlMarkupKey.Namespace, _file, _logger, out string relativeNamespace);

            CollectEnumSchemas(node, enumContractName, relativeNamespace);

            if (Definitions.Any()) 
                return;

            if (enumContractName != null)
                _logger.LogError($"Could not parse definition for enum '{enumContractName}'", enumContractName.Location);
            else
                _logger.LogError("Could not detect any enum definition", enumElement.Location);
        }

        private void CollectEnumSchemas(CreateTableStatement createTableStatement, Token<string> rootEnumContractName, string relativeNamespace)
        {
            TableDefinition definition = createTableStatement.Definition;
            IEnumerable<CheckConstraintDefinition> checkConstraints = definition.TableConstraints
                                                                                .Concat(definition.ColumnDefinitions.SelectMany(x => x.Constraints))
                                                                                .OfType<CheckConstraintDefinition>();
            
            foreach (CheckConstraintDefinition checkConstraint in checkConstraints)
            {
                CollectEnumSchema(definition, checkConstraint, rootEnumContractName, relativeNamespace);
            }
        }

        private void CollectEnumSchema(TableDefinition tableDefinition, CheckConstraintDefinition checkConstraint, Token<string> rootEnumContractName, string relativeNamespace)
        {
            bool hasExplicitContractName = false;
            ISqlMarkupDeclaration markup = SqlMarkupReader.Read(checkConstraint, SqlMarkupCommentKind.SingleLine, _file, _logger);
            if (markup.TryGetSingleElementValue(SqlMarkupKey.Enum, _file, _logger, out Token<string> enumContractName))
            {
                if (rootEnumContractName != null)
                {
                    _logger.LogError("Ambiguous enum contract definition name. When declaring check constraints as enums, the root enum declaration should not define a name.", enumContractName.Location);
                    return;
                }
                hasExplicitContractName = true;
            }
            else
                enumContractName = rootEnumContractName;

            if (enumContractName == null)
                return;

            // Do not collect multiple enum schemas for the same contract name
            // This ensures that only the first check constraint is considered when the root enum contract name declaration is set
            if (_definitionMap.ContainsKey(enumContractName))
                return;

            TargetPath targetPath = PathUtility.BuildAbsoluteTargetName(_productName, _areaName, LayerName.DomainModel, relativeNamespace, targetNamePath: enumContractName);
            EnumSchema schema = new EnumSchema(targetPath.AbsoluteNamespace, enumContractName, SchemaDefinitionSource.AutoGenerated, enumContractName.Location);
            BooleanExpression checkCondition = checkConstraint.CheckCondition.SkipParenthesis();

            switch (checkCondition)
            {
                /*
                 [id] IN (101 -- Feature1
                        , 102 -- Feature2
                        , 103 -- Feature3
                 )
                */
                case InPredicate { Expression: ColumnReferenceExpression columnReferenceExpression } inPredicate when inPredicate.Values.All(x => x is Literal):
                    CollectSchemaMembers(schema, inPredicate, tableDefinition, columnReferenceExpression);
                    break;

                /*
                 ([id]=(103)) -- Feature3
              OR  [id]=(101)  -- Feature1
              OR ([id]=(102)) -- Feature2
              OR ([id]=(104) AND [name] = N'Feature4'
                 )
                */
                case BooleanBinaryExpression booleanBinaryExpression when TryCollectSchemaMembers(schema, booleanBinaryExpression, tableDefinition):
                    break;

                // [id]=(101)
                case BooleanComparisonExpression booleanComparisonExpression when TryCollectSchemaMembers(schema, booleanComparisonExpression, nameComparisonExpression: null, tableDefinition):
                    break;

                default:
                    if (hasExplicitContractName)
                        _logger.LogError($"Could not parse definition for enum '{enumContractName}'", enumContractName.Location);

                    return;
            }

            _definitionMap.Add(enumContractName, schema);
        }

        private static bool TryCollectSchemaMembers(EnumSchema schema, BooleanComparisonExpression flagComparisonExpression, BooleanComparisonExpression nameComparisonExpression, TableDefinition tableDefinition)
        {
            // [id]
            if (flagComparisonExpression.FirstExpression is not ColumnReferenceExpression columnReferenceExpression)
                return false;

            CollectBaseType(schema, tableDefinition, columnReferenceExpression);

            // 104
            ScalarExpression flagExpression = flagComparisonExpression.SecondExpression.SkipParenthesis();
            if (flagExpression is not IntegerLiteral flagLiteral)
                return false;

            // Feature4
            StringLiteral nameLiteral = null;
            if (nameComparisonExpression is { FirstExpression: ColumnReferenceExpression, SecondExpression: StringLiteral stringLiteral }) 
                nameLiteral = stringLiteral;

            if (!TryCollectSchemaMember(schema, flagExpression, flagLiteral, nameLiteral))
                return false;

            return true;
        }

        private static bool TryCollectSchemaMembers(EnumSchema schema, BooleanBinaryExpression booleanBinaryExpression, TableDefinition tableDefinition)
        {
            // "[id]=(102)"
            // "[id]=(104)" "[name] = N'Feature4'"
            // ...
            ICollection<BooleanBinaryRecord> records = Flatten(booleanBinaryExpression).ToArray();
            return records.Any() && records.All(x => TryCollectSchemaMembers(schema, x.FlagComparison, x.NameComparison, tableDefinition));
        }

        private static void CollectSchemaMembers(EnumSchema schema, InPredicate inPredicate, TableDefinition tableDefinition, ColumnReferenceExpression columnReferenceExpression)
        {
            CollectBaseType(schema, tableDefinition, columnReferenceExpression);
            foreach (Literal literal in inPredicate.Values.Cast<Literal>())
            {
                if (!TryCollectSchemaMember(schema, container: literal, flagLiteral: literal, nameLiteral: null))
                    return;
            }
        }

        private static void CollectBaseType(EnumSchema schema, TableDefinition tableDefinition, ColumnReferenceExpression columnReferenceExpression)
        {
            ColumnDefinition column = tableDefinition.ColumnDefinitions.FirstOrDefault(x => x.ColumnIdentifier.Value == columnReferenceExpression.GetName().Value);
            if (column is not { DataType: SqlDataTypeReference sqlDataTypeReference } || !PrimitiveTypeMap.TryGetPrimitiveType(sqlDataTypeReference.SqlDataTypeOption, out PrimitiveType dataType)) 
                return;

            SourceLocation location = new SourceLocation(schema.Location.Source, sqlDataTypeReference.StartLine, sqlDataTypeReference.StartColumn);
            schema.BaseType = new PrimitiveTypeReference(dataType, isNullable: false, isEnumerable: false, location);
        }

        private static bool TryCollectSchemaMember(EnumSchema schema, TSqlFragment container, Literal flagLiteral, Literal nameLiteral)
        {
            if (!Int32.TryParse(flagLiteral.Value, out int memberValue))
                return false;

            string memberName;

            if (nameLiteral != null)
                memberName = nameLiteral.Value;
            else if (TryParseMemberNameFromComment(container, out string memberNameFromComment))
                memberName = memberNameFromComment;
            else
                return false;

            schema.Members.Add(new EnumSchemaMember(memberName, memberValue, stringValue: memberValue.ToString(CultureInfo.InvariantCulture), schema));
            return true;
        }

        private static bool TryParseMemberNameFromComment(TSqlFragment node, out string name)
        {
            for (int i = node.LastTokenIndex + 1; i < node.ScriptTokenStream.Count; i++)
            {
                TSqlParserToken token = node.ScriptTokenStream[i];
                switch (token.TokenType)
                {
                    case TSqlTokenType.WhiteSpace:
                        continue;

                    case TSqlTokenType.SingleLineComment:
                        Match match = Regex.Match(token.Text, @"^-- (?<name>[^\d][\w]{0,})$");
                        name = match.Groups["name"].Value;
                        return !String.IsNullOrWhiteSpace(name);

                    case TSqlTokenType.RightParenthesis:
                        continue;

                    default:
                        name = null;
                        return false;
                }
            }

            name = null;
            return false;
        }

        private static bool TryParseContractNameFromComment(TSqlFragment node, out string name)
        {
            for (int i = node.FirstTokenIndex - 1; i >= 0; i--)
            {
                TSqlParserToken token = node.ScriptTokenStream[i];
                switch (token.TokenType)
                {
                    case TSqlTokenType.WhiteSpace:
                        continue;

                    case TSqlTokenType.SingleLineComment:
                        Match match = Regex.Match(token.Text, @"^-- (?<name>[^\d][\w]{0,})$");
                        name = match.Groups["name"].Value;
                        return !String.IsNullOrWhiteSpace(name);

                    default:
                        name = null;
                        return false;
                }
            }

            name = null;
            return false;
        }

        private static IEnumerable<BooleanBinaryRecord> Flatten(BooleanBinaryExpression booleanBinaryExpression)
        {
            if (booleanBinaryExpression.BinaryExpressionType != BooleanBinaryExpressionType.Or)
                yield break;

            BooleanExpression firstExpression = booleanBinaryExpression.FirstExpression.SkipParenthesis();
            BooleanExpression secondExpression = booleanBinaryExpression.SecondExpression.SkipParenthesis();

            if (firstExpression is BooleanBinaryExpression anotherBooleanBinaryExpression)
            {
                foreach (BooleanBinaryRecord record in Flatten(anotherBooleanBinaryExpression))
                {
                    yield return record;
                }
            }

            foreach (BooleanExpression booleanExpression in EnumerableExtensions.Create(firstExpression, secondExpression))
            {
                BooleanBinaryRecord record = CollectBinaryRecord(booleanExpression);
                if (record != null)
                    yield return record;
            }
        }

        private static BooleanBinaryRecord CollectBinaryRecord(BooleanExpression booleanExpression)
        {
            BooleanExpression flagExpression = booleanExpression;
            BooleanComparisonExpression nameComparisonExpression = null;

            // [id]=(104) AND [name] = N'Feature4'
            if (booleanExpression is BooleanBinaryExpression { BinaryExpressionType: BooleanBinaryExpressionType.And } booleanBinaryExpression)
            {
                BooleanExpression secondExpression = booleanBinaryExpression.SecondExpression.SkipParenthesis();
                if (secondExpression is BooleanComparisonExpression booleanComparisonExpression)
                {
                    flagExpression = booleanBinaryExpression.FirstExpression.SkipParenthesis();
                    nameComparisonExpression = booleanComparisonExpression;
                }
            }

            // [id]=(103)
            if (flagExpression is BooleanComparisonExpression flagComparisonExpression)
                return new BooleanBinaryRecord(flagComparisonExpression, nameComparisonExpression);

            return null;
        }

        [DebuggerDisplay("{FlagComparisonDebug} {NameComparisonDebug}")]
        private sealed class BooleanBinaryRecord
        {
            private string FlagComparisonDebug => FlagComparison.Dump();
            private string NameComparisonDebug => NameComparison?.Dump();

            public BooleanComparisonExpression FlagComparison { get; }
            public BooleanComparisonExpression NameComparison { get; }

            public BooleanBinaryRecord(BooleanComparisonExpression flagComparison, BooleanComparisonExpression nameComparison)
            {
                FlagComparison = flagComparison;
                NameComparison = nameComparison;
            }
        }
    }
}