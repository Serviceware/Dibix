using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using SqlDataType = Microsoft.SqlServer.Dac.Model.SqlDataType;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class IndexSizeSqlCodeAnalysisRule : SqlCodeAnalysisRule<IndexSizeSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 32;
        public override string ErrorMessage => "{0} index {1} size is {2} bytes. The maximum key length is {3} bytes";
    }

    public sealed class IndexSizeSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        private const short MaximumClusteredIndexSize    = 900;  // bytes
        private const short MaximumNonClusteredIndexSize = 1700; // bytes
        private const byte  BitLength                    = 1;    // byte
        private const byte  TinyIntLength                = 1;    // byte
        private const byte  SmallIntLength               = 2;    // bytes
        private const byte  IntLength                    = 4;    // bytes
        private const byte  BigIntLength                 = 8;    // bytes
        private const byte  DateLength                   = 3;    // bytes
        private const byte  DateTimeLength               = 8;    // bytes
        private const short SysNameLength                = 256;  // bytes
        private const short UniqueIdentifierLength       = 16;   // bytes

        // helpLine suppressions
        private static readonly HashSet<string> Workarounds = new HashSet<string>
        {
            "PK_hlspparentprocattrmapping"
          , "UQ_hlsysportalconfig_cfg1"
        };

        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            foreach (Constraint constraint in base.Model.GetConstraints(node.SchemaObjectName))
            {
                if (constraint.Kind != ConstraintKind.PrimaryKey && constraint.Kind != ConstraintKind.Unique)
                    continue;

                this.Check(constraint.Source, constraint.IsClustered.Value, constraint.Name, constraint.Columns);
            }

            foreach (Index index in base.Model.GetIndexes(node.SchemaObjectName))
            {
                this.Check(index.Source, index.IsClustered, index.Name, index.Columns);
            }
        }

        private void Check(SourceInformation source, bool isClustered, string indexName, IEnumerable<Column> columns)
        {
            string kind;
            int maxSize;
            if (isClustered)
            {
                kind = "Clustered";
                maxSize = MaximumClusteredIndexSize;
            }
            else
            {
                kind = "Nonclustered";
                maxSize = MaximumNonClusteredIndexSize;
            }

            int length = columns.Where(x => !x.IsComputed)
                                .Select(ComputeColumnLength)
                                .Sum();

            if (length <= maxSize)
                return;

            if (!Workarounds.Contains(indexName))
                base.Fail(source, kind, indexName, length, maxSize);
        }

        private static int ComputeColumnLength(Column column)
        {
            switch (column.SqlDataType)
            {
                case SqlDataType.Bit:
                    return BitLength;

                case SqlDataType.TinyInt:
                    return TinyIntLength;

                case SqlDataType.SmallInt:
                    return SmallIntLength;

                case SqlDataType.Int:
                    return IntLength;

                case SqlDataType.BigInt:
                    return BigIntLength;

                case SqlDataType.Date:
                    return DateLength;

                case SqlDataType.DateTime:
                    return DateTimeLength;

                case SqlDataType.Decimal:
                    int precision = column.Precision;
                    if (precision > 0 && precision < 10)
                        return 5;
                    else if (precision > 9 && precision < 20)
                        return 9;
                    else if (precision > 19 && precision < 29)
                        return 13;
                    else if (precision > 28 && precision < 39)
                        return 17;
                    else
                        throw new ArgumentOutOfRangeException(nameof(column.Precision), column.Precision, "Invalid decimal precision");

                case SqlDataType.Char:
                case SqlDataType.VarChar:
                    return column.Length;

                case SqlDataType.NChar:
                case SqlDataType.NVarChar:
                    return column.Length * 2;

                case SqlDataType.UniqueIdentifier:
                    return UniqueIdentifierLength;

                case SqlDataType.Binary:
                case SqlDataType.VarBinary:
                    return column.Length;

                case SqlDataType.Unknown when column.DataTypeName == "sys.sysname":
                    return SysNameLength;

                default:
                    throw new ArgumentOutOfRangeException(nameof(column.DataTypeName), column.DataTypeName, "Unexpected data type for clustered index column");
            }
        }
    }
}