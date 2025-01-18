using System;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Data.SqlClient;

namespace Dibix.Http.Server.Tests
{
    internal static class SqlExceptionFactory
    {
        private static readonly CreateSqlException Factory = CompileFactory();

        public static SqlException Create(string serverVersion, int infoNumber, byte errorState, byte errorClass, string server, string errorMessage, string procedure, int lineNumber)
        {
            return Factory(serverVersion, infoNumber, errorState, errorClass, server, errorMessage, procedure, lineNumber);
        }

        private static CreateSqlException CompileFactory()
        {
            Type sqlErrorType = typeof(SqlError);
            Type sqlErrorCollectionType = typeof(SqlErrorCollection);
            Type sqlExceptionType = typeof(SqlException);

            ParameterExpression serverVersionParameter = Expression.Parameter(typeof(string), "serverVersion");
            ParameterExpression infoNumberParameter = Expression.Parameter(typeof(int), "infoNumber");
            ParameterExpression errorStateParameter = Expression.Parameter(typeof(byte), "errorState");
            ParameterExpression errorClassParameter = Expression.Parameter(typeof(byte), "errorClass");
            ParameterExpression serverParameter = Expression.Parameter(typeof(string), "server");
            ParameterExpression errorMessageParameter = Expression.Parameter(typeof(string), "errorMessage");
            ParameterExpression procedureParameter = Expression.Parameter(typeof(string), "procedure");
            ParameterExpression lineNumberParameter = Expression.Parameter(typeof(int), "lineNumber");
            Expression exceptionParameter = Expression.Constant(null, typeof(Exception));

            ConstructorInfo sqlErrorConstructor = sqlErrorType.GetConstructorSafe
            (
                BindingFlags.NonPublic | BindingFlags.Instance
              , infoNumberParameter.Type
              , errorStateParameter.Type
              , errorClassParameter.Type
              , serverParameter.Type
              , errorMessageParameter.Type
              , procedureParameter.Type
              , lineNumberParameter.Type
              , exceptionParameter.Type
            );

            if (sqlErrorConstructor == null)
                throw new InvalidOperationException($"Could not find ctor on type '{sqlErrorType}'");

            ParameterExpression errorVariable = Expression.Variable(sqlErrorType, "error");
            Expression errorValue = Expression.New
            (
                sqlErrorConstructor
              , infoNumberParameter
              , errorStateParameter
              , errorClassParameter
              , serverParameter
              , errorMessageParameter
              , procedureParameter
              , lineNumberParameter
              , exceptionParameter
            );
            Expression errorAssign = Expression.Assign(errorVariable, errorValue);

            ParameterExpression errorCollectionVariable = Expression.Variable(sqlErrorCollectionType, "errorCollection");
            Expression errorCollectionValue = Expression.ListInit(Expression.New(sqlErrorCollectionType), errorVariable);
            Expression errorCollectionAssign = Expression.Assign(errorCollectionVariable, errorCollectionValue);

            ParameterExpression exceptionVariable = Expression.Variable(sqlExceptionType, "exception");
            Expression exceptionValue = Expression.Call(sqlExceptionType, "CreateException", Type.EmptyTypes, errorCollectionVariable, serverVersionParameter);
            Expression exceptionAssign = Expression.Assign(exceptionVariable, exceptionValue);


            Expression block = Expression.Block
            (
                new[]
                {
                    errorVariable
                  , errorCollectionVariable
                  , exceptionVariable
                }
              , errorAssign
              , errorCollectionAssign
              , exceptionAssign
            );

            Expression<CreateSqlException> lambda = Expression.Lambda<CreateSqlException>
            (
                block
              , serverVersionParameter
              , infoNumberParameter
              , errorStateParameter
              , errorClassParameter
              , serverParameter
              , errorMessageParameter
              , procedureParameter
              , lineNumberParameter
            );
            CreateSqlException compiled = lambda.Compile();
            return compiled;
        }

        private delegate SqlException CreateSqlException(string serverVersion, int infoNumber, byte errorState, byte errorClass, string server, string errorMessage, string procedure, int lineNumber);
    }
}