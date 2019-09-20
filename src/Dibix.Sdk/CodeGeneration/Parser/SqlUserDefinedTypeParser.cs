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
        private readonly IErrorReporter _errorReporter;
        private readonly string _productName;
        private readonly string _areaName;

        public SqlUserDefinedTypeParser(IErrorReporter errorReporter, string productName, string areaName)
        {
            this._errorReporter = errorReporter;
            this._productName = productName;
            this._areaName = areaName;
        }

        public UserDefinedTypeDefinition Parse(string filePath)
        {
            TSqlFragment fragment = ScriptDomFacade.Load(filePath);
            UserDefinedTypeVisitor visitor = new UserDefinedTypeVisitor(() => SqlHintParser.FromFragment(filePath, this._errorReporter, fragment).ToArray(), this._productName, this._areaName);
            fragment.Accept(visitor);
            return visitor.Definition;
        }

        private static string GenerateDisplayName(string udtTypeName)
        {
            const string delimiter = "_udt_";
            int index = udtTypeName.IndexOf(delimiter, StringComparison.Ordinal);
            if (index >= 0)
                udtTypeName = udtTypeName.Substring(index + delimiter.Length);

            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(udtTypeName).Replace("_", String.Empty);
        }

        private class UserDefinedTypeVisitor : TSqlFragmentVisitor
        {
            private readonly Lazy<ICollection<SqlHint>> _hintsAccessor;
            private readonly string _productName;
            private readonly string _areaName;

            public UserDefinedTypeDefinition Definition { get; private set; }

            public UserDefinedTypeVisitor(Func<ICollection<SqlHint>> hintsProvider, string productName, string areaName)
            {
                this._productName = productName;
                this._areaName = areaName;
                this._hintsAccessor = new Lazy<ICollection<SqlHint>>(hintsProvider);
            }

            public override void Visit(CreateTypeTableStatement node)
            {
                string typeName = node.Name.BaseIdentifier.Value;
                string @namespace = this._hintsAccessor.Value.SingleHintValue(SqlHint.Namespace);
                string displayName = this._hintsAccessor.Value.SingleHintValue(SqlHint.Name);
                if (String.IsNullOrEmpty(displayName))
                    displayName = GenerateDisplayName(typeName);

                @namespace = NamespaceUtility.BuildFullNamespace(this._productName, this._areaName, LayerName.Data, @namespace);
                ICollection<string> notNullableColumns = new HashSet<string>(GetNotNullableColumns(node.Definition));
                this.Definition = new UserDefinedTypeDefinition(typeName, @namespace, displayName);
                this.Definition.Columns.AddRange(node.Definition.ColumnDefinitions.Select(x => MapColumn(x, notNullableColumns)));
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

            private static UserDefinedTypeColumn MapColumn(ColumnDefinition column, ICollection<string> notNullableColumns)
            {
                Type clrType = column.DataType.ToClrType();
                string columnName = column.ColumnIdentifier.Value;
                bool shouldBeNullable = !notNullableColumns.Contains(columnName);
                bool isNullable = clrType.IsNullable();
                bool makeNullable = shouldBeNullable && !isNullable;
                if (makeNullable)
                    clrType = clrType.MakeNullable();

                return new UserDefinedTypeColumn(columnName, clrType.ToCSharpTypeName());
            }
        }
    }
}
