using System;
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

            SourceLocation schemaLocation = enumContractName.Location;
            TargetPath targetPath = PathUtility.BuildAbsoluteTargetName(_productName, _areaName, LayerName.DomainModel, relativeNamespace, targetNamePath: enumContractName);
            BooleanExpression checkCondition = checkConstraint.CheckCondition.SkipParenthesis();
            PrimitiveTypeReference baseType = null;
            ICollection<EnumSchemaMember> members = new List<EnumSchemaMember>();

            switch (checkCondition)
            {
                /*
                 [id] IN (101 -- Feature1
                        , 102 -- Feature2
                        , 103 -- Feature3
                 )
                */
                case InPredicate { Expression: ColumnReferenceExpression columnReferenceExpression } inPredicate when inPredicate.Values.All(x => x is Literal):
                    CollectSchemaMembers(schemaLocation, inPredicate, tableDefinition, columnReferenceExpression, members, ref baseType);
                    break;

                /*
                 ([id]=(103)) -- Feature3
              OR  [id]=(101)  -- Feature1
              OR ([id]=(102)) -- Feature2
              OR ([id]=(104) AND [name] = N'Feature4'
                 )
                */
                case BooleanBinaryExpression booleanBinaryExpression when TryCollectSchemaMembers(schemaLocation, booleanBinaryExpression, tableDefinition, members, ref baseType):
                    break;

                // [id]=(101)
                case BooleanComparisonExpression booleanComparisonExpression when TryCollectSchemaMember(schemaLocation, booleanComparisonExpression, nameComparisonExpression: null, tableDefinition, members, ref baseType):
                    break;

                default:
                    if (hasExplicitContractName)
                        _logger.LogError($"Could not parse definition for enum '{enumContractName}'", schemaLocation);

                    return;
            }

            if (members.All(x => x.ActualValue != 0))
                members.Add(new EnumSchemaMember("None", 0, "0", usesMemberReference: false));

            EnumSchema schema = new EnumSchema(members, targetPath.AbsoluteNamespace, targetPath.RelativeNamespace, enumContractName, SchemaDefinitionSource.AutoGenerated, schemaLocation, baseType);
            _definitionMap.Add(enumContractName, schema);
        }

        private static bool TryCollectSchemaMember(SourceLocation schemaLocation, BooleanComparisonExpression flagComparisonExpression, BooleanComparisonExpression nameComparisonExpression, TableDefinition tableDefinition, ICollection<EnumSchemaMember> target, ref PrimitiveTypeReference baseType)
        {
            // [id]
            if (flagComparisonExpression.FirstExpression is not ColumnReferenceExpression columnReferenceExpression)
                return false;

            CollectBaseType(schemaLocation, tableDefinition, columnReferenceExpression, ref baseType);

            // 104
            ScalarExpression flagExpression = flagComparisonExpression.SecondExpression.SkipParenthesis();
            if (flagExpression is not IntegerLiteral flagLiteral)
                return false;

            // Feature4
            StringLiteral nameLiteral = null;
            if (nameComparisonExpression is { FirstExpression: ColumnReferenceExpression, SecondExpression: StringLiteral stringLiteral })
                nameLiteral = stringLiteral;

            if (!TryCollectSchemaMember(flagExpression, flagLiteral, nameLiteral, target))
                return false;

            return true;
        }

        private static bool TryCollectSchemaMembers(SourceLocation schemaLocation, BooleanBinaryExpression booleanBinaryExpression, TableDefinition tableDefinition, ICollection<EnumSchemaMember> target, ref PrimitiveTypeReference baseType)
        {
            // "[id]=(102)"
            // "[id]=(104)" "[name] = N'Feature4'"
            // ...
            ICollection<BooleanBinaryRecord> records = Flatten(booleanBinaryExpression).ToArray();
            bool result = false;
            foreach (BooleanBinaryRecord record in records)
            {
                if (!TryCollectSchemaMember(schemaLocation, record.FlagComparison, record.NameComparison, tableDefinition, target, ref baseType))
                    continue;

                result = true;
            }
            return result;
        }

        private static void CollectSchemaMembers(SourceLocation schemaLocation, InPredicate inPredicate, TableDefinition tableDefinition, ColumnReferenceExpression columnReferenceExpression, ICollection<EnumSchemaMember> target, ref PrimitiveTypeReference baseType)
        {
            CollectBaseType(schemaLocation, tableDefinition, columnReferenceExpression, ref baseType);
            foreach (Literal literal in inPredicate.Values.Cast<Literal>())
            {
                if (!TryCollectSchemaMember(container: literal, flagLiteral: literal, nameLiteral: null, target))
                    return;
            }
        }

        private static void CollectBaseType(SourceLocation schemaLocation, TableDefinition tableDefinition, ColumnReferenceExpression columnReferenceExpression, ref PrimitiveTypeReference baseType)
        {
            ColumnDefinition column = tableDefinition.ColumnDefinitions.FirstOrDefault(x => x.ColumnIdentifier.Value == columnReferenceExpression.GetName().Value);
            if (column is not { DataType: SqlDataTypeReference sqlDataTypeReference } || !PrimitiveTypeMap.TryGetPrimitiveType(sqlDataTypeReference.SqlDataTypeOption, out PrimitiveType dataType))
                return;

            SourceLocation baseTypeLocation = new SourceLocation(schemaLocation.Source, sqlDataTypeReference.StartLine, sqlDataTypeReference.StartColumn);
            baseType = new PrimitiveTypeReference(dataType, isNullable: false, isEnumerable: false, size: null, location: baseTypeLocation);
        }

        private static bool TryCollectSchemaMember(TSqlFragment container, Literal flagLiteral, Literal nameLiteral, ICollection<EnumSchemaMember> target)
        {
            if (!Int32.TryParse(flagLiteral.Value, out int memberValue))
                return false;

            string memberName;

            if (nameLiteral != null)
            {
                memberName = nameLiteral.Value;
            }
            else if (TryParseMemberNameFromComment(container, out string memberNameFromComment))
            {
                memberName = memberNameFromComment;
            }
            else
            {
                return false;
            }

            EnumSchemaMember member = new EnumSchemaMember(memberName, memberValue, stringValue: memberValue.ToString(CultureInfo.InvariantCulture), usesMemberReference: false);
            target.Add(member);
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