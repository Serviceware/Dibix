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

        private class UserDefinedTypeVisitor : TSqlFragmentVisitor
        {
            private readonly Lazy<ICollection<SqlHint>> _hintsAccessor;

            public UserDefinedTypeDefinition Definition { get; private set; }

            public UserDefinedTypeVisitor(Func<ICollection<SqlHint>> hintsProvider)
            {
                this._hintsAccessor = new Lazy<ICollection<SqlHint>>(hintsProvider);
            }

            public override void Visit(CreateTypeTableStatement node)
            {
                string typeName = node.Name.BaseIdentifier.Value;
                string displayName = this._hintsAccessor.Value.SingleHintValue(SqlHint.Name);
                if (String.IsNullOrEmpty(displayName))
                    displayName = GenerateDisplayName(typeName);

                this.Definition = new UserDefinedTypeDefinition(typeName, displayName);
                this.Definition.Columns.AddRange(node.Definition.ColumnDefinitions.Select(x => MapColumn(node.Definition, x)));
            }

            private static UserDefinedTypeColumn MapColumn(TableDefinition table, ColumnDefinition column)
            {
                Type clrType = column.DataType.ToClrType();
                string columnName = column.ColumnIdentifier.Value;
                bool shouldBeNullable = table.IsNullable(columnName);
                bool isNullable = clrType.IsNullable();
                bool makeNullable = shouldBeNullable && !isNullable;
                if (makeNullable)
                    clrType = clrType.MakeNullable();

                return new UserDefinedTypeColumn(columnName, clrType.ToCSharpTypeName());
            }
        }
    }
}
