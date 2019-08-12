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

        public SqlUserDefinedTypeParser(IErrorReporter errorReporter)
        {
            this._errorReporter = errorReporter;
        }

        public UserDefinedTypeDefinition Parse(string filePath)
        {
            TSqlFragment fragment = ScriptDomFacade.Load(filePath);
            UserDefinedTypeVisitor visitor = new UserDefinedTypeVisitor(() => SqlHintParser.FromFragment(filePath, this._errorReporter, fragment).ToArray());
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

        private class UserDefinedTypeVisitor : ConstraintVisitor
        {
            private readonly Lazy<ICollection<SqlHint>> _hintsAccessor;

            public UserDefinedTypeDefinition Definition { get; private set; }

            public UserDefinedTypeVisitor(Func<ICollection<SqlHint>> hintsProvider)
            {
                this._hintsAccessor = new Lazy<ICollection<SqlHint>>(hintsProvider);
            }

            protected override void Visit(Table table)
            {
                if (table.Type != TableType.TypeTable)
                    return;

                base.Visit(table);

                string typeName = table.Name.BaseIdentifier.Value;
                string displayName = this._hintsAccessor.Value.SingleHintValue(SqlHint.Name);
                if (String.IsNullOrEmpty(displayName))
                    displayName = GenerateDisplayName(typeName);

                ICollection<string> notNullableColumns = new HashSet<string>();
                if (base.Targets.TryGetValue(table.Name.ToKey(), out ConstraintTarget constraintTarget))
                {
                    notNullableColumns.AddRange(constraintTarget.Constraints
                                                                .Where(x => x.Type == ConstraintType.PrimaryKey
                                                                         || x.Type == ConstraintType.Nullable && !((NullableConstraintDefinition)x.Definition).Nullable)
                                                                .SelectMany(x => x.Columns, (x, y) => y.Name));
                }

                this.Definition = new UserDefinedTypeDefinition(typeName, displayName);
                this.Definition.Columns.AddRange(table.Definition.ColumnDefinitions.Select(x => MapColumn(x, notNullableColumns)));
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
