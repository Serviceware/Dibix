using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
            this._productName = productName;
            this._areaName = areaName;
            this._typeResolver = typeResolver;
            this._logger = logger;
        }

        public UserDefinedTypeSchema Parse(string filePath)
        {
            TSqlFragment fragment = ScriptDomFacade.Load(filePath);
            UserDefinedTypeVisitor visitor = new UserDefinedTypeVisitor(this._productName, this._areaName, filePath, fragment, this._typeResolver, this._logger);
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
                this._productName = productName;
                this._areaName = areaName;
                this._source = source;
                this._typeResolver = typeResolver;
                this._logger = logger;
                this._markupAccessor = new Lazy<ISqlMarkupDeclaration>(() => SqlMarkupReader.ReadHeader(fragment, source, logger));
            }

            public override void Visit(CreateTypeTableStatement node)
            {
                string typeName = node.Name.ToFullName();
                _ = this._markupAccessor.Value.TryGetSingleElementValue(SqlMarkupKey.Namespace, this._source, this._logger, out string relativeNamespace);
                
                if (!this._markupAccessor.Value.TryGetSingleElementValue(SqlMarkupKey.Name, this._source, this._logger, out string definitionName))
                    definitionName = GenerateDefinitionName(typeName);

                string @namespace = NamespaceUtility.BuildAbsoluteNamespace(this._productName, this._areaName, LayerName.Data, relativeNamespace);
                ICollection<string> notNullableColumns = new HashSet<string>(GetNotNullableColumns(node.Definition));
                this.Definition = new UserDefinedTypeSchema(@namespace, definitionName, typeName);
                this.Definition.Properties.AddRange(node.Definition.ColumnDefinitions.Select(x => this.MapColumn(x, relativeNamespace, notNullableColumns)));
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
                string columnName = column.ColumnIdentifier.Value;
                bool isNullable = !notNullableColumns.Contains(columnName);
                TypeReference typeReference = column.DataType.ToTypeReference(isNullable, columnName, relativeNamespace, this._source, this._markupAccessor.Value, this._typeResolver, this._logger, out string udtName);
                return new ObjectSchemaProperty(columnName, typeReference);
            }
        }
    }
}
