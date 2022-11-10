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
            private readonly Lazy<ISqlMarkupDeclaration> _markupAccessor;
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
                _markupAccessor = new Lazy<ISqlMarkupDeclaration>(() => SqlMarkupReader.ReadHeader(fragment, source, logger));
            }

            public override void Visit(CreateTypeTableStatement node)
            {
                string typeName = node.Name.ToFullName();
                _ = _markupAccessor.Value.TryGetSingleElementValue(SqlMarkupKey.Namespace, _source, _logger, out string relativeNamespace);
                
                if (!_markupAccessor.Value.TryGetSingleElementValue(SqlMarkupKey.Name, _source, _logger, out string definitionName))
                    definitionName = GenerateDefinitionName(typeName);

                NamespacePath @namespace = PathUtility.BuildAbsoluteNamespace(_productName, _areaName, LayerName.Data, relativeNamespace);
                ICollection<string> notNullableColumns = new HashSet<string>(GetNotNullableColumns(node.Definition));
                IList<ObjectSchemaProperty> properties = node.Definition
                                                             .ColumnDefinitions
                                                             .Select(x => MapColumn(x, relativeNamespace, notNullableColumns))
                                                             .ToArray();
                Definition = new UserDefinedTypeSchema(@namespace.Path, definitionName, SchemaDefinitionSource.Defined, typeName, properties);
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
                    if (!(constraint is UniqueConstraintDefinition unique) || !unique.IsPrimaryKey)
                        continue;

                    foreach (ColumnWithSortOrder column in unique.Columns)
                        yield return column.Column.GetName().Value;
                }

                foreach (ColumnDefinition column in table.ColumnDefinitions)
                {
                    if (column.Constraints.Any(x => x is NullableConstraintDefinition nullable && !nullable.Nullable))
                        yield return column.ColumnIdentifier.Value;
                }
            }

            private ObjectSchemaProperty MapColumn(ColumnDefinition column, string relativeNamespace, ICollection<string> notNullableColumns)
            {
                Identifier columnIdentifier = column.ColumnIdentifier;
                string columnName = columnIdentifier.Value;
                bool isNullable = !notNullableColumns.Contains(columnName);
                TypeReference typeReference = column.DataType.ToTypeReference(isNullable, columnName, relativeNamespace, _source, _markupAccessor.Value, _typeResolver, _logger, out string udtName);
                return new ObjectSchemaProperty(name: new Token<string>(columnName, _source, columnIdentifier.StartLine, columnIdentifier.StartColumn), typeReference);
            }
        }
    }
}