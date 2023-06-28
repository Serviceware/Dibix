using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.SqlServer.Server;

namespace Dibix
{
    [Serializable]
    public sealed class DatabaseAccessException : Exception
    {
        public CommandType CommandType { get; }
        public string CommandText { get; }
        public ParametersVisitor Parameters { get; }
        public string ParameterDump { get; }
        public string SqlDebugStatement { get; }
        public DatabaseAccessErrorCode AdditionalErrorCode { get; }

        private DatabaseAccessException(string message, CommandType commandType, string commandText, ParametersVisitor parameters, string parameterDump, string sqlDebugStatement, DatabaseAccessErrorCode additionalErrorCode, Exception innerException) : base(message, innerException)
        {
            CommandType = commandType;
            CommandText = commandText;
            Parameters = parameters;
            ParameterDump = parameterDump;
            SqlDebugStatement = sqlDebugStatement;
            AdditionalErrorCode = additionalErrorCode;
        }

        public override string ToString()
        {
            string additionalInfo = SqlDebugStatement ?? ParameterDump;

            if (additionalInfo?.Length > 0)
            {
                additionalInfo = $@"

Debug statement:
{additionalInfo}";
            }

            string text = $"{base.ToString()}{additionalInfo}";
            return text;
        }

        internal static DatabaseAccessException Create(CommandType commandType, string commandText, ParametersVisitor parameters, Exception innerException, bool isSqlClient)
        {
            return Create(innerException.Message, commandType, commandText, parameters, innerException, additionalErrorCode: DatabaseAccessErrorCode.None, isSqlClient);
        }
        internal static DatabaseAccessException Create(string message, CommandType commandType, string commandText, ParametersVisitor parameters, DatabaseAccessErrorCode additionalErrorCode, bool isSqlClient)
        {
            return Create(message, commandType, commandText, parameters, innerException: null, additionalErrorCode, isSqlClient);
        }
        private static DatabaseAccessException Create(string message, CommandType commandType, string commandText, ParametersVisitor parameters, Exception innerException, DatabaseAccessErrorCode additionalErrorCode, bool isSqlClient)
        {
            string newMessage = @$"{message}
CommandType: {commandType}
CommandText: {(commandType == CommandType.StoredProcedure ? commandText : "<Inline>")}";

            ICollection<ParameterDescriptor> parameterDescriptors = CollectParameters(parameters);
            string parameterDump = parameterDescriptors.Any() ? CollectParameterDump(parameterDescriptors) : null;
            string sqlDebugStatement = isSqlClient ? CollectSqlDebugStatement(commandType, commandText, parameterDescriptors) : null;

            return new DatabaseAccessException(newMessage, commandType, commandText, parameters, parameterDump, sqlDebugStatement, additionalErrorCode, innerException);
        }

        private static string CollectParameterDump(IEnumerable<ParameterDescriptor> parameters)
        {
            string FormatParameter(ParameterDescriptor parameter)
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("Parameter ").Append(parameter.Name);

                string parameterType;
                string parameterDescription = null;

                if (parameter.Value is StructuredType structuredType)
                {
                    parameterType = structuredType.TypeName;
                    parameterDescription = structuredType.Dump();
                }
                else
                    parameterType = parameter.Type.ToString();

                if (parameterType != null)
                {
                    sb.Append('(')
                      .Append(parameterType)
                      .Append(')');
                }

                sb.Append(":");

                if (parameter.Value is not StructuredType)
                {
                    sb.Append(' ')
                      .Append(parameter.Value ?? "NULL");
                }

                if (parameterDescription != null)
                {
                    sb.AppendLine()
                      .Append(parameterDescription);
                }

                string parameterInfo = sb.ToString();
                return parameterInfo;
            }

            string parameterInfo = String.Join(Environment.NewLine, parameters.Select(FormatParameter));
            return parameterInfo;
        }

        private static string CollectSqlDebugStatement(CommandType commandType, string commandText, ICollection<ParameterDescriptor> parameters)
        {
            string debugStatement = CollectParameterDeclarations(parameters, initialize: true);

            if (commandType == CommandType.StoredProcedure)
            {
                string separator = debugStatement.Length > 0 ? $"{Environment.NewLine}{Environment.NewLine}" : null;
                debugStatement = $"{debugStatement}{separator}{CollectStoredProcedureDebugStatement(commandText, parameters)}";
            }

            return debugStatement;
        }

        private static string CollectStoredProcedureDebugStatement(string procedureName, ICollection<ParameterDescriptor> parameters)
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

        private static string CollectParameterDeclarations(IEnumerable<ParameterDescriptor> parameters, bool initialize, params DbType[] dbTypeFilter)
        {
            ICollection<ParameterDescriptor> filteredParameters = parameters.Where(x => !dbTypeFilter.Any() || dbTypeFilter.Contains(x.Type)).ToArray();
            if (!filteredParameters.Any())
                return "";

            string FormatParameterDeclaration(string declaration, int maxDeclarationLength, ParameterDescriptor parameter)
            {
                if (!initialize || parameter.Type == DbType.Object)
                    return declaration;

                string declarationWithInitializer = $"{declaration.PadRight(maxDeclarationLength)} = {GetConstantLiteral(parameter.Value)}";
                return declarationWithInitializer;
            }

            int maxParameterLength = filteredParameters.Max(x => x.Name.Length);
            var parameterDeclarations = filteredParameters.Select(x => new { Declaration = $"DECLARE @{x.Name.PadRight(maxParameterLength)} {ToDataTypeString(x.Type, x.Value, length: null)}", Parameter = x }).ToArray();
            int maxDeclarationLength = parameterDeclarations.Max(x => x.Declaration.Length);
            string udtInitializers = CollectUdtInitializers(filteredParameters);
            string debugStatements = String.Join(Environment.NewLine, parameterDeclarations.Select(x => FormatParameterDeclaration(x.Declaration, maxDeclarationLength, x.Parameter)));
            if (udtInitializers.Length > 0)
            {
                debugStatements = $@"{debugStatements}
{udtInitializers}";
            }

            return debugStatements;
        }

        private static string CollectUdtInitializers(IEnumerable<ParameterDescriptor> parameters)
        {
            string[] CollectValues(SqlDataRecord record)
            {
                object[] values = new object[record.FieldCount];
                record.GetValues(values);
                string[] strValues = values.Select(GetConstantLiteral).ToArray();
                return strValues;
            }

            string GenerateUdtInitializer(ParameterDescriptor parameter)
            {
                if (parameter.Value is not StructuredType structuredType)
                    throw new InvalidOperationException($"Unexpected db parameter value for type 'Object': {parameter.Value} ({parameter.Value?.GetType()})");

                ICollection<SqlMetaData> metadata = structuredType.GetMetadata().ToArray();
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

        private static string NormalizeIdentifier(string identifier)
        {
            Guard.IsNotNullOrEmpty(identifier, nameof(identifier));
            IList<string> parts = identifier.Split('.').Select(x => x.TrimStart('[').TrimEnd(']')).ToList();
            return String.Join(".", parts.Select(x => $"[{x}]"));
        }

        private static string GetConstantLiteral(object value) => value switch
        {
            null or DBNull => "NULL",
            StructuredType => throw new ArgumentOutOfRangeException(nameof(value), value, null),
            byte[] binary => GetConstantLiteral(binary),
            Stream stream => GetConstantLiteral(stream),
            string or char or Guid or Uri or XElement => $"N'{value}'",
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

        private static string ToDataTypeString(DbType dbType, object value, int? length)
        {
            string lengthStr = $"{length?.ToString() ?? "MAX"}";
            switch (dbType)
            {
                case DbType.AnsiString: return $"VARCHAR({lengthStr})";
                case DbType.Binary: return $"VARBINARY({lengthStr})";
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
                case DbType.String: return $"NVARCHAR({lengthStr})";
                case DbType.Time: return "TIME";
                case DbType.UInt16: return "SMALLINT";
                case DbType.UInt32: return "INT";
                case DbType.UInt64: return "BIGINT";
                case DbType.VarNumeric: return "INT";
                case DbType.AnsiStringFixedLength: return $"CHAR({length})";
                case DbType.StringFixedLength: return $"NCHAR({length})";
                case DbType.Xml: return "XML";
                case DbType.DateTime2: return "DATETIME2";
                case DbType.DateTimeOffset: return "DATETIMEOFFSET";
                default: return $"<unknown({nameof(DbType)}.{dbType})>"; // throw new ArgumentOutOfRangeException(nameof(dbType), dbType, null);
            }
        }

        private static ICollection<ParameterDescriptor> CollectParameters(ParametersVisitor parameters)
        {
            ICollection<ParameterDescriptor> statements = new Collection<ParameterDescriptor>();
            parameters.VisitInputParameters((name, type, value, _, _) => statements.Add(new ParameterDescriptor(name, type, value)));
            return statements;
        }

        private readonly struct ParameterDescriptor
        {
            public string Name { get; }
            public DbType Type { get; }
            public object Value { get; }

            public ParameterDescriptor(string name, DbType type, object value)
            {
                Name = name;
                Type = type;
                Value = value;
            }
        }
    }
}