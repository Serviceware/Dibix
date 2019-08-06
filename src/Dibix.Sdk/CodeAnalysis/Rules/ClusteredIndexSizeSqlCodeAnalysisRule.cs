using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class ClusteredIndexSizeSqlCodeAnalysisRule : SqlCodeAnalysisRule<ClusteredIndexSizeSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 32;
        public override string ErrorMessage => "Clustered index {0} size is {1} bytes. The maximum key length is {2} bytes";
    }

    public sealed class ClusteredIndexSizeSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        private const short MaximumClusteredIndexSize = 900; // bytes
        private const byte  BitLength                 = 1;   // byte
        private const byte  TinyIntLength             = 1;   // byte
        private const byte  SmallIntLength            = 2;   // bytes
        private const byte  IntLength                 = 4;   // bytes
        private const byte  BigIntLength              = 8;   // bytes
        private const byte  DateLength                = 3;   // bytes
        private const byte  DateTimeLength            = 8;   // bytes
        private const short SysNameLength             = 256; // bytes
        private const short UniqueIdentifierLength    = 16;  // bytes
        private const byte  DefaultBinaryLength       = 1;
        private const byte  DefaultCharacterLength    = 1;
        private const byte  DefaultDecimalPrecision   = 18;

        // helpLine suppressions
        private static readonly HashSet<string> Workarounds = new HashSet<string>
        {
            "PK_hlspparentprocattrmapping"
        };

        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            foreach (Constraint constraint in this.GetConstraints(node.SchemaObjectName))
            {
                if (!(constraint.Definition is UniqueConstraintDefinition uniqueConstraint))
                    continue;
                
                bool isClustered = uniqueConstraint.Clustered ?? uniqueConstraint.IsPrimaryKey;
                if (!isClustered)
                    continue;

                int length = CollectColumnLengths(node.Definition, constraint).Sum();
                if (length <= MaximumClusteredIndexSize)
                    continue;

                string constraintName = uniqueConstraint.ConstraintIdentifier.Value;

                if (!Workarounds.Contains(constraintName))
                    base.Fail(uniqueConstraint, constraintName, length, MaximumClusteredIndexSize);
            }
        }

        private static IEnumerable<int> CollectColumnLengths(TableDefinition definition, Constraint primaryKey)
        {
            ICollection<string> columnNames = new HashSet<string>(primaryKey.Columns.Select(x => x.Name));
            foreach (ColumnDefinition column in definition.ColumnDefinitions)
            {
                if (!columnNames.Contains(column.ColumnIdentifier.Value) || column.IsPersisted)
                    continue;

                switch (column.DataType)
                {
                    case SqlDataTypeReference sqlDataTypeReference:
                        yield return DetermineByteLength(sqlDataTypeReference);
                        break;

                    case UserDataTypeReference userDataTypeReference when userDataTypeReference.Name.BaseIdentifier.Value.ToUpperInvariant() == "SYSNAME":
                        yield return SysNameLength;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(column.DataType), column.DataType, "Unexpected data type for clustered index column");
                }
            }
        }

        private static int DetermineByteLength(SqlDataTypeReference sqlDataTypeReference)
        {
            switch (sqlDataTypeReference.SqlDataTypeOption)
            {
                case SqlDataTypeOption.Bit:
                    return BitLength;

                case SqlDataTypeOption.TinyInt:
                    return TinyIntLength;

                case SqlDataTypeOption.SmallInt:
                    return SmallIntLength;

                case SqlDataTypeOption.Int:
                    return IntLength;

                case SqlDataTypeOption.BigInt:
                    return BigIntLength;

                case SqlDataTypeOption.Date:
                    return DateLength;

                case SqlDataTypeOption.DateTime:
                    return DateTimeLength;

                case SqlDataTypeOption.Decimal:
                    int precision = GetLengthSpecification(sqlDataTypeReference, DefaultDecimalPrecision);
                    if (precision > 0 && precision < 10)
                        return 5;
                    else if (precision > 9 && precision < 20)
                        return 9;
                    else if (precision > 19 && precision < 29)
                        return 13;
                    else if (precision > 28 && precision < 39)
                        return 17;
                    else
                        throw new ArgumentOutOfRangeException(nameof(sqlDataTypeReference.Parameters), precision, "Invalid decimal precision");

                case SqlDataTypeOption.Char:
                case SqlDataTypeOption.VarChar:
                    return GetLengthSpecification(sqlDataTypeReference, DefaultCharacterLength);

                case SqlDataTypeOption.NChar:
                case SqlDataTypeOption.NVarChar:
                    return GetLengthSpecification(sqlDataTypeReference, DefaultCharacterLength) * 2;

                case SqlDataTypeOption.UniqueIdentifier:
                    return UniqueIdentifierLength;

                case SqlDataTypeOption.Binary:
                case SqlDataTypeOption.VarBinary:
                    return GetLengthSpecification(sqlDataTypeReference, DefaultBinaryLength);

                default:
                    throw new ArgumentOutOfRangeException(nameof(sqlDataTypeReference.SqlDataTypeOption), sqlDataTypeReference.SqlDataTypeOption, "Unexpected data type for clustered index column");
            }
        }

        private static int GetLengthSpecification(SqlDataTypeReference sqlDataTypeReference, int @default)
        {
            if (!sqlDataTypeReference.Parameters.Any())
                return @default;

            IntegerLiteral literal = (IntegerLiteral)sqlDataTypeReference.Parameters[0];
            return Convert.ToInt32(literal.Value, CultureInfo.InvariantCulture);
        }
    }
}