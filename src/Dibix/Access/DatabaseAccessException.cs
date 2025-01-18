using System;
using System.Data;
using System.Globalization;

namespace Dibix
{
    [Serializable]
    public sealed class DatabaseAccessException : Exception
    {
        private readonly string _parameterDumpTruncated;
        private readonly string _sqlDebugStatementTruncated;

        public CommandType CommandType { get; }
        public string CommandText { get; }
        public ParametersVisitor Parameters { get; }
        public string ParameterDump { get; }
        public string SqlDebugStatement { get; }
        public DatabaseAccessErrorCode AdditionalErrorCode { get; }
        public int? SqlErrorNumber { get; }

        private DatabaseAccessException(string message, CommandType commandType, string commandText, ParametersVisitor parameters, string parameterDump, string parameterDumpTruncated, string sqlDebugStatement, string sqlDebugStatementTruncated, DatabaseAccessErrorCode additionalErrorCode, int? sqlErrorNumber, Exception innerException) : base(message, innerException)
        {
            _parameterDumpTruncated = parameterDumpTruncated;
            _sqlDebugStatementTruncated = sqlDebugStatementTruncated;
            CommandType = commandType;
            CommandText = commandText;
            Parameters = parameters;
            ParameterDump = parameterDump;
            SqlDebugStatement = sqlDebugStatement;
            AdditionalErrorCode = additionalErrorCode;
            SqlErrorNumber = sqlErrorNumber;
        }

        public override string ToString()
        {
            string additionalInfo = _sqlDebugStatementTruncated ?? _parameterDumpTruncated;

            if (additionalInfo?.Length > 0)
            {
                additionalInfo = $@"

Debug statement:
{additionalInfo}";
            }

            string text = $"{base.ToString()}{additionalInfo}";
            return text;
        }

        internal static DatabaseAccessException Create(CommandType commandType, string commandText, ParametersVisitor parameters, Exception innerException, int? sqlErrorNumber, bool isSqlClient)
        {
            return Create(innerException.Message, commandType, commandText, parameters, innerException, additionalErrorCode: DatabaseAccessErrorCode.None, sqlErrorNumber, isSqlClient);
        }
        internal static DatabaseAccessException Create(string message, CommandType commandType, string commandText, ParametersVisitor parameters, DatabaseAccessErrorCode additionalErrorCode, bool isSqlClient)
        {
            return Create(message, commandType, commandText, parameters, innerException: null, additionalErrorCode, sqlErrorNumber: null, isSqlClient);
        }
        internal static DatabaseAccessException Create(DatabaseAccessErrorCode errorCode, string commandText, CommandType commandType, ParametersVisitor parameters, bool isSqlClient, params object[] args)
        {
            string message = String.Format(CultureInfo.InvariantCulture, GetExceptionMessageTemplate(errorCode), args);
            return Create(message, commandType, commandText, parameters, errorCode, isSqlClient);
        }
        private static DatabaseAccessException Create(string message, CommandType commandType, string commandText, ParametersVisitor parameters, Exception innerException, DatabaseAccessErrorCode additionalErrorCode, int? sqlErrorNumber, bool isSqlClient)
        {
            string newMessage = @$"{message}
CommandType: {commandType}
CommandText: {(commandType == CommandType.StoredProcedure ? commandText : "<Inline>")}";

            string parameterDump = DbParameterFormatter.CollectParameterDump(parameters, truncate: false);
            string parameterDumpTruncated = DbParameterFormatter.CollectParameterDump(parameters, truncate: true);
            string sqlDebugStatement = null;
            string sqlDebugStatementTruncated = null;
            if (isSqlClient)
            {
                sqlDebugStatement = DbParameterFormatter.CollectSqlDebugStatement(commandType, commandText, parameters, truncate: false);
                sqlDebugStatementTruncated = DbParameterFormatter.CollectSqlDebugStatement(commandType, commandText, parameters, truncate: true);
            }
            return new DatabaseAccessException(newMessage, commandType, commandText, parameters, parameterDump, parameterDumpTruncated, sqlDebugStatement, sqlDebugStatementTruncated, additionalErrorCode, sqlErrorNumber, innerException);
        }

        private static string GetExceptionMessageTemplate(DatabaseAccessErrorCode errorCode) => errorCode switch
        {
            DatabaseAccessErrorCode.SequenceContainsNoElements => "Sequence contains no elements",
            DatabaseAccessErrorCode.SequenceContainsMoreThanOneElement => "Sequence contains more than one element",
            DatabaseAccessErrorCode.ParameterSizeExceeded => "Length of parameter '{0}' is '{1}', which exceeds the supported size '{2}'",
            _ => throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, null)
        };
    }
}