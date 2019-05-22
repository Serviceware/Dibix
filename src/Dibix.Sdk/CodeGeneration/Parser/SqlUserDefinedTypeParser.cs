using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class SqlUserDefinedTypeParser
    {
        public UserDefinedTypeDefinition Parse(string filePath)
        {
            TSqlParser parser = new TSql140Parser(true);
            using (Stream stream = File.OpenRead(filePath))
            {
                using (TextReader reader = new StreamReader(stream))
                {
                    TSqlFragment fragment = parser.Parse(reader, out IList<ParseError> _);
                    UserDefinedTypeVisitor visitor = new UserDefinedTypeVisitor();
                    fragment.Accept(visitor);
                    return visitor.Definition;
                }
            }
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
            public UserDefinedTypeDefinition Definition { get; private set; }

            public override void Visit(CreateTypeTableStatement node)
            {
                string typeName = node.Name.BaseIdentifier.Value;
                string displayName = node.SingleHint(SqlHint.Name);
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
