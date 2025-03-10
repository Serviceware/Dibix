using System;
using System.Data;
using System.Globalization;

namespace Dibix
{
    public sealed class DatabaseAccessException : Exception
    {
        private readonly string _parameterDumpTruncated;
        private readonly string _tSqlDebugStatementTruncated;

        public CommandType CommandType { get; }
        public string CommandText { get; }
        public ParametersVisitor Parameters { get; }
        public string ParameterDump { get; }
        public string TSqlDebugStatement { get; }
        public DatabaseAccessErrorCode AdditionalErrorCode { get; }
        public int? SqlErrorNumber { get; }

        private DatabaseAccessException(string message, CommandType commandType, string commandText, ParametersVisitor parameters, string parameterDump, string parameterDumpTruncated, string tSqlDebugStatement, string tSqlDebugStatementTruncated, DatabaseAccessErrorCode additionalErrorCode, int? sqlErrorNumber, Exception innerException) : base(message, innerException)
        {
            _parameterDumpTruncated = parameterDumpTruncated;
            _tSqlDebugStatementTruncated = tSqlDebugStatementTruncated;
            CommandType = commandType;
            CommandText = commandText;
            Parameters = parameters;
            ParameterDump = parameterDump;
            TSqlDebugStatement = tSqlDebugStatement;
            AdditionalErrorCode = additionalErrorCode;
            SqlErrorNumber = sqlErrorNumber;
        }

        public override string ToString()
        {
            string additionalInfo = _tSqlDebugStatementTruncated ?? _parameterDumpTruncated;

            if (additionalInfo?.Length > 0)
            {
                additionalInfo = $@"

Debug statement:
{additionalInfo}";
            }

            string text = $"{base.ToString()}{additionalInfo}";
            return text;
        }

        internal static DatabaseAccessException Create(CommandType commandType, string commandText, ParametersVisitor parameters, Exception innerException, int? sqlErrorNumber, bool collectTSqlDebugStatement)
        {
            return Create(innerException.Message, commandType, commandText, parameters, innerException, additionalErrorCode: DatabaseAccessErrorCode.None, sqlErrorNumber, collectTSqlDebugStatement);
        }
        internal static DatabaseAccessException Create(string message, CommandType commandType, string commandText, ParametersVisitor parameters, DatabaseAccessErrorCode additionalErrorCode, bool collectTSqlDebugStatement)
        {
            return Create(message, commandType, commandText, parameters, innerException: null, additionalErrorCode, sqlErrorNumber: null, collectTSqlDebugStatement);
        }
        internal static DatabaseAccessException Create(DatabaseAccessErrorCode errorCode, string commandText, CommandType commandType, ParametersVisitor parameters, bool collectTSqlDebugStatement, params object[] args)
        {
            string message = String.Format(CultureInfo.InvariantCulture, GetExceptionMessageTemplate(errorCode), args);
            return Create(message, commandType, commandText, parameters, errorCode, collectTSqlDebugStatement);
        }
        private static DatabaseAccessException Create(string message, CommandType commandType, string commandText, ParametersVisitor parameters, Exception innerException, DatabaseAccessErrorCode additionalErrorCode, int? sqlErrorNumber, bool collectTSqlDebugStatement)
        {
            string newMessage = @$"{message}
CommandType: {commandType}
CommandText: {(commandType == CommandType.StoredProcedure ? commandText : "<Inline>")}";

            string parameterDump = SqlDiagnosticsUtility.CollectParameterDump(parameters, truncate: false);
            string parameterDumpTruncated = SqlDiagnosticsUtility.CollectParameterDump(parameters, truncate: true);
            string tSqlDebugStatement = null;
            string tSqlDebugStatementTruncated = null;
            if (collectTSqlDebugStatement)
            {
                tSqlDebugStatement = TSqlDebugStatementFormatter.CollectTSqlDebugStatement(commandType, commandText, parameters, truncate: false);
                tSqlDebugStatementTruncated = TSqlDebugStatementFormatter.CollectTSqlDebugStatement(commandType, commandText, parameters, truncate: true);
            }
            return new DatabaseAccessException(newMessage, commandType, commandText, parameters, parameterDump, parameterDumpTruncated, tSqlDebugStatement, tSqlDebugStatementTruncated, additionalErrorCode, sqlErrorNumber, innerException);
        }

        private static string GetExceptionMessageTemplate(DatabaseAccessErrorCode errorCode) => errorCode switch
        {
            DatabaseAccessErrorCode.SequenceContainsNoElements => "Sequence contains no elements",
            DatabaseAccessErrorCode.SequenceContainsMoreThanOneElement => "Sequence contains more than one element",
            _ => throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, null)
        };
    }
}