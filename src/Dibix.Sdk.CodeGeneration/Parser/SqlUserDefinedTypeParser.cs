﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class SqlUserDefinedTypeParser
    {
        private readonly ILogger _logger;
        private readonly string _productName;
        private readonly string _areaName;
        private readonly ITypeResolverFacade _typeResolver;

        public SqlUserDefinedTypeParser(string productName, string areaName, ITypeResolverFacade typeResolver, ILogger logger)
        {
            _productName = productName;
            _areaName = areaName;
            _typeResolver = typeResolver;
            _logger = logger;
        }

        public UserDefinedTypeSchema Parse(string filePath)
        {
            TSqlFragment fragment = ScriptDomFacade.Load(filePath);
            UserDefinedTypeVisitor visitor = new UserDefinedTypeVisitor(_productName, _areaName, filePath, fragment, _typeResolver, _logger);
            fragment.Accept(visitor);
            return visitor.Definition;
        }

        private class UserDefinedTypeVisitor : TSqlFragmentVisitor
        {
            private readonly string _productName;
            private readonly string _areaName;
            private readonly string _source;
            private readonly ITypeResolverFacade _typeResolver;
            private readonly ILogger _logger;

            public UserDefinedTypeSchema Definition { get; private set; }

            public UserDefinedTypeVisitor(string productName, string areaName, string source, TSqlFragment fragment, ITypeResolverFacade typeResolver, ILogger logger)
            {
                _productName = productName;
                _areaName = areaName;
                _source = source;
                _typeResolver = typeResolver;
                _logger = logger;
            }

            public override void Visit(CreateTypeTableStatement node)
            {
                string typeName = node.Name.ToFullName();
                ISqlMarkupDeclaration markup = SqlMarkupReader.Read(node, SqlMarkupCommentKind.SingleLine, _source, _logger);
                _ = markup.TryGetSingleElementValue(SqlMarkupKey.Namespace, _source, _logger, out string relativeNamespace);
                
                if (!markup.TryGetSingleElementValue(SqlMarkupKey.Name, _source, _logger, out string definitionName))
                    definitionName = GenerateDefinitionName(typeName);

                NamespacePath @namespace = PathUtility.BuildAbsoluteNamespace(_productName, _areaName, LayerName.Data, relativeNamespace);
                ICollection<string> notNullableColumns = new HashSet<string>(GetNotNullableColumns(node.Definition));
                IList<ObjectSchemaProperty> properties = node.Definition
                                                             .ColumnDefinitions
                                                             .Select(x => MapColumn(x, relativeNamespace, notNullableColumns, markup))
                                                             .ToArray();
                Definition = new UserDefinedTypeSchema(@namespace.Path, @namespace.RelativeNamespace, definitionName, SchemaDefinitionSource.AutoGenerated, new SourceLocation(_source, node.StartLine, node.StartColumn), typeName, properties);
            }

            private static string GenerateDefinitionName(string udtName)
            {
                const string delimiter = "_udt_";
                string udtBaseName = udtName.Split('.').Select(x => x.TrimStart('[').TrimEnd(']')).Last();
                int index = udtBaseName.IndexOf(delimiter, StringComparison.Ordinal);
                if (index >= 0)
                    udtBaseName = udtBaseName.Substring(index + delimiter.Length);

                return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(udtBaseName).Replace("_", String.Empty);
            }

            private static IEnumerable<string> GetNotNullableColumns(TableDefinition table)
            {
                foreach (ConstraintDefinition constraint in table.TableConstraints)
                {
                    if (constraint is not UniqueConstraintDefinition { IsPrimaryKey: true } unique)
                        continue;

                    foreach (ColumnWithSortOrder column in unique.Columns)
                        yield return column.Column.GetName().Value;
                }

                foreach (ColumnDefinition column in table.ColumnDefinitions)
                {
                    if (column.Constraints.Any(x => x is NullableConstraintDefinition { Nullable: false }))
                        yield return column.ColumnIdentifier.Value;
                }
            }

            private ObjectSchemaProperty MapColumn(ColumnDefinitionBase column, string relativeNamespace, ICollection<string> notNullableColumns, ISqlMarkupDeclaration markup)
            {
                Identifier columnIdentifier = column.ColumnIdentifier;
                string columnName = columnIdentifier.Value;
                bool isNullable = !notNullableColumns.Contains(columnName);
                TypeReference typeReference = column.DataType.ToTypeReference(isNullable, columnName, relativeNamespace, _source, markup, _typeResolver, _logger, out string udtName);
                long? maxLength = null;
                byte? precision = null;
                byte? scale = null;
                CollectDataTypeParameters(column, ref maxLength, ref precision, ref scale);
                return new ObjectSchemaProperty(name: new Token<string>(columnName, new SourceLocation(_source, columnIdentifier.StartLine, columnIdentifier.StartColumn)), typeReference, maxLength: maxLength, precision: precision, scale: scale);
            }

            private static void CollectDataTypeParameters(ColumnDefinitionBase column, ref long? maxLength, ref byte? precision, ref byte? scale)
            {
                if (column.DataType is not ParameterizedDataTypeReference dataType)
                    return;

                switch (dataType.Parameters.Count)
                {
                    case 1:
                        maxLength = CollectMaxLength(dataType.Parameters[0]);
                        break;

                    case 2:
                        CollectPrecisionAndScale(dataType.Parameters[0], dataType.Parameters[1], ref precision, ref scale);
                        break;
                }
            }

            private static long? CollectMaxLength(Literal parameter)
            {
                if (parameter is not IntegerLiteral integerLiteral)
                    return null;

                if (!Int64.TryParse(integerLiteral.Value, out long maxLength))
                    return null;

                return maxLength;
            }

            private static void CollectPrecisionAndScale(Literal parameter1, Literal parameter2, ref byte? precision, ref byte? scale)
            {
                if (parameter1 is not IntegerLiteral literal1)
                    return;

                if (parameter2 is not IntegerLiteral literal2)
                    return;

                if (!Byte.TryParse(literal1.Value, out byte precisionValue))
                    return;

                if (!Byte.TryParse(literal2.Value, out byte scaleValue))
                    return;

                precision = precisionValue;
                scale = scaleValue;
            }
        }
    }
}
