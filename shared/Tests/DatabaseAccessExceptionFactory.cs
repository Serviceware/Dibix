using System;
using System.Data;
using System.Reflection;
using Dibix.Http.Server.Tests;
using Microsoft.Data.SqlClient;
using Moq;
using Moq.Language.Flow;

namespace Dibix.Tests
{
    internal static class DatabaseAccessExceptionFactory
    {
        public static Exception CreateException(int errorInfoNumber, string errorMessage, CommandType? commandType = null, string? commandText = null, bool collectUdtParameterValues = true, Action<InputParameterVisitor>? inputParameterVisitor = null)
        {
            Mock<ParametersVisitor> parametersVisitor = new Mock<ParametersVisitor>(MockBehavior.Strict);
            ISetup<ParametersVisitor> parametersVisitorSetup = parametersVisitor.Setup(x => x.VisitInputParameters(It.IsAny<InputParameterVisitor>()));
            if (inputParameterVisitor != null)
                parametersVisitorSetup.Callback(inputParameterVisitor);

            SqlException sqlException = SqlExceptionFactory.Create(serverVersion: null, infoNumber: errorInfoNumber, errorState: 0, errorClass: 0, server: null, errorMessage, procedure: null, lineNumber: 0);
            const bool collectTSqlDebugStatement = true;

            MethodInfo createMethod = typeof(DatabaseAccessException).SafeGetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static, [typeof(CommandType), typeof(string), typeof(ParametersVisitor), typeof(Exception), typeof(int?), typeof(bool), typeof(bool)]);
            Exception exception = (Exception)createMethod.Invoke(null, [commandType, commandText, parametersVisitor.Object, sqlException, sqlException.Number, collectTSqlDebugStatement, collectUdtParameterValues])!;
            return exception;
        }
        public static Exception CreateException(DatabaseAccessErrorCode errorCode, string errorMessage, CommandType? commandType = null, string? commandText = null)
        {
            ParametersVisitor parametersVisitor = ParametersVisitor.Empty;
            const bool collectTSqlDebugStatement = true;
            const bool collectUdtParameterValues = true;
            MethodInfo createMethod = typeof(DatabaseAccessException).SafeGetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static, [typeof(string), typeof(CommandType), typeof(string), typeof(ParametersVisitor), typeof(DatabaseAccessErrorCode), typeof(bool), typeof(bool)]);
            Exception exception = (Exception)createMethod.Invoke(null, [errorMessage, commandType, commandText, parametersVisitor, errorCode, collectTSqlDebugStatement, collectUdtParameterValues])!;
            return exception;
        }
    }
}