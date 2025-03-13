using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
#if NET
using Microsoft.Data.SqlClient.Server;
#else
using Microsoft.SqlServer.Server;
#endif

namespace Dibix
{
    internal static class TSqlDebugStatementFormatter
    {
        public static string CollectTSqlDebugStatement(CommandType commandType, string commandText, ParametersVisitor parameters, bool truncate)
        {
            ICollection<ParameterDescriptor> parameterDescriptors = SqlDiagnosticsUtility.CollectParameters(parameters);
            string debugStatement = CollectTSqlParameterDeclarations(parameterDescriptors, initialize: true, truncate: truncate);

            if (commandType == CommandType.StoredProcedure)
            {
                string separator = debugStatement.Length > 0 ? $"{Environment.NewLine}{Environment.NewLine}" : null;
                debugStatement = $"{debugStatement}{separator}{CollectTSqlStoredProcedureDebugStatement(commandText, parameterDescriptors)}";
            }

            return debugStatement;
        }

        private static string CollectTSqlStoredProcedureDebugStatement(string procedureName, ICollection<ParameterDescriptor> parameters)
        {
            string debugStatement = $"EXEC {procedureName}";

            if (parameters.Any())
            {
                string padding = new string(' ', procedureName.Length + 4);
                int maxParameterLength = parameters.Max(x => x.Name.Length);
                string parameterInitializers = String.Join($"{Environment.NewLine}{padding}, ", parameters.Select(x => $"@{x.Name.PadRight(maxParameterLength)} = @{x.Name}"));
                debugStatement = $"{debugStatement} {parameterInitializers}";
            }

            return debugStatement;
        }

        private static string CollectTSqlParameterDeclarations(IEnumerable<ParameterDescriptor> parameters, bool initialize, bool truncate, params DbType[] dbTypeFilter)
        {
            ICollection<ParameterDescriptor> filteredParameters = parameters.Where(x => !dbTypeFilter.Any() || dbTypeFilter.Contains(x.Type)).ToArray();
            if (!filteredParameters.Any())
                return "";

            string FormatParameterDeclaration(string declaration, int maxDeclarationLength, ParameterDescriptor parameter)
            {
                if (!initialize || parameter.Type == DbType.Object)
                    return declaration;

                string declarationWithInitializer = $"{declaration.PadRight(maxDeclarationLength)} = {GetConstantLiteral(parameter.Value, truncate)}";
                return declarationWithInitializer;
            }

            int maxParameterLength = filteredParameters.Max(x => x.Name.Length);
            var parameterDeclarations = filteredParameters.Select(x => new { Declaration = $"DECLARE @{x.Name.PadRight(maxParameterLength)} {ToDataTypeString(x.Type, x.Value, x.Size)}", Parameter = x }).ToArray();
            int maxDeclarationLength = parameterDeclarations.Max(x => x.Declaration.Length);
            string udtInitializers = CollectUdtInitializers(filteredParameters, truncate);
            string debugStatements = String.Join(Environment.NewLine, parameterDeclarations.Select(x => FormatParameterDeclaration(x.Declaration, maxDeclarationLength, x.Parameter)));
            if (udtInitializers.Length > 0)
            {
                debugStatements = $@"{debugStatements}
{udtInitializers}";
            }

            return debugStatements;
        }

        private static string CollectUdtInitializers(IEnumerable<ParameterDescriptor> parameters, bool truncate)
        {
            string[] CollectValues(SqlDataRecord record)
            {
                object[] values = new object[record.FieldCount];
                record.GetValues(values);
                string[] strValues = values.Select(value => GetConstantLiteral(value, truncate)).ToArray();
                return strValues;
            }

            string GenerateUdtInitializer(ParameterDescriptor parameter)
            {
                if (parameter.Value is not StructuredType structuredType)
                    throw new InvalidOperationException($"Unexpected db parameter value for type 'Object': {parameter.Value} ({parameter.Value?.GetType()})");

                IReadOnlyCollection<SqlMetaData> metadata = structuredType.GetMetadata();
                string[][] rows = structuredType.GetRecords().Select(CollectValues).ToArray();
                if (!rows.Any())
                    return "";

                IList<int> maxLengths = metadata.Select((x, i) => Math.Max(rows.Max(y => y[i].Length), x.Name.Length + 2)).ToArray();
                string padding = new string(' ', parameter.Name.Length);
                string initializer = $@"INSERT INTO @{parameter.Name} ({String.Join(", ", metadata.Select((x, i) => $"[{x.Name}]".PadRight(maxLengths[i])))})
      {padding} VALUES {String.Join($"{Environment.NewLine}{padding}            , ", rows.Select(x => $"({String.Join(", ", x.Select((y, i) => y.PadRight(maxLengths[i])))})"))}";
                return initializer;
            }

            string debugStatement = String.Join(Environment.NewLine, parameters.Where(x => x.Type == DbType.Object).Select(GenerateUdtInitializer));
            return debugStatement;
        }

        private static string GetConstantLiteral(object value, bool truncate) => value switch
        {
            null or DBNull => "NULL",
            StructuredType => throw new ArgumentOutOfRangeException(nameof(value), value, null),
            byte[] binary => GetConstantLiteral(binary),
            Stream stream => GetConstantLiteral(stream),
            string or char or Guid or Uri or XElement => $"N'{SqlDiagnosticsUtility.TrimParameterValueIfNecessary(value, truncate).Replace("'", "''")}'",
            DateTime dateTime => $"CAST(N'{dateTime:s}.{dateTime:fff}' AS DATETIME)",
            DateTimeOffset dateTimeOffset => $"CAST(N'{dateTimeOffset:O}' AS DATETIMEOFFSET)",
            bool boolValue => boolValue ? "1" : "0",
            Enum => Convert.ToInt32(value).ToString(),
            IConvertible convertible => convertible.ToString(CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
        private static string GetConstantLiteral(Stream stream)
        {
            if (stream.CanSeek)
                stream.Position = 0;

            byte[] binary;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                binary = memoryStream.ToArray();
            }
            return GetConstantLiteral(binary);
        }
        private static string GetConstantLiteral(IReadOnlyCollection<byte> binary)
        {
            string value = binary.Count <= 100 ? $"0x{String.Join(null, binary.Select(x => $"{x:x2}"))}" : $"<TRUNCATED> /* {binary.Count} bytes */";
            return value;
        }

        private static string NormalizeIdentifier(string identifier)
        {
            Guard.IsNotNullOrEmpty(identifier, nameof(identifier));
            IList<string> parts = identifier.Split('.').Select(x => x.TrimStart('[').TrimEnd(']')).ToList();
            return String.Join(".", parts.Select(x => $"[{x}]"));
        }

        private static string ToDataTypeString(DbType dbType, object value, int? size)
        {
            string sizeStr = $"{size?.ToString() ?? "MAX"}";
            switch (dbType)
            {
                case DbType.AnsiString: return $"VARCHAR({sizeStr})";
                case DbType.Binary: return $"VARBINARY({sizeStr})";
                case DbType.Byte: return "TINYINT";
                case DbType.Boolean: return "BIT";
                case DbType.Currency: return "MONEY";
                case DbType.Date: return "DATE";
                case DbType.DateTime: return "DATETIME";
                case DbType.Decimal: return "DECIMAL";
                case DbType.Double: return "FLOAT";
                case DbType.Guid: return "UNIQUEIDENTIFIER";
                case DbType.Int16: return "SMALLINT";
                case DbType.Int32: return "INT";
                case DbType.Int64: return "BIGINT";
                case DbType.Object when value is StructuredType structuredType: return NormalizeIdentifier(structuredType.TypeName);
                case DbType.SByte: return "TINYINT";
                case DbType.Single: return "FLOAT";
                case DbType.String: return $"NVARCHAR({sizeStr})";
                case DbType.Time: return "TIME";
                case DbType.UInt16: return "SMALLINT";
                case DbType.UInt32: return "INT";
                case DbType.UInt64: return "BIGINT";
                case DbType.VarNumeric: return "INT";
                case DbType.AnsiStringFixedLength: return $"CHAR({size})";
                case DbType.StringFixedLength: return $"NCHAR({size})";
                case DbType.Xml: return "XML";
                case DbType.DateTime2: return "DATETIME2";
                case DbType.DateTimeOffset: return "DATETIMEOFFSET";
                default: return $"<unknown({nameof(DbType)}.{dbType})>"; // throw new ArgumentOutOfRangeException(nameof(dbType), dbType, null);
            }
        }
    }
}