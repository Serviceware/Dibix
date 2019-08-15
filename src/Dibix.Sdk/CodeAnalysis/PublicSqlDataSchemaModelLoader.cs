using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Dibix.Sdk.CodeGeneration;
using Microsoft.Build.Framework;
using Microsoft.Data.Tools.Schema.Extensibility;
using Microsoft.SqlServer.Dac.Model;
using Assembly = System.Reflection.Assembly;

namespace Dibix.Sdk.CodeAnalysis
{
    internal static class PublicSqlDataSchemaModelLoader
    {
        private static readonly Assembly SchemaSqlAssembly = typeof(IExtension).Assembly;
        private static readonly LoadPublicDataSchemaModel ModelFactory = CompileModelFactory();

        public static TSqlModel Load(string databaseSchemaProviderName, string modelCollation, ITaskItem[] source, ITaskItem[] sqlReferencePath, ITask task, IErrorReporter errorReporter)
        {
            return ModelFactory(databaseSchemaProviderName, modelCollation, source, sqlReferencePath, task, errorReporter);
        }

        private static LoadPublicDataSchemaModel CompileModelFactory()
        {
            // (string databaseSchemaProviderName, ITaskItem[] source, ITaskItem[] sqlReferencePath, ITask task, IErrorReporter errorReporter) =>
            ParameterExpression databaseSchemaProviderNameParameter = Expression.Parameter(typeof(string), "databaseSchemaProviderName");
            ParameterExpression modelCollationParameter = Expression.Parameter(typeof(string), "modelCollation");
            ParameterExpression sourceParameter = Expression.Parameter(typeof(ITaskItem[]), "source");
            ParameterExpression sqlReferencePathParameter = Expression.Parameter(typeof(ITaskItem[]), "sqlReferencePath");
            ParameterExpression taskParameter = Expression.Parameter(typeof(ITask), "task");
            ParameterExpression errorReporterParameter = Expression.Parameter(typeof(IErrorReporter), "errorReporter");

            Type hostLoaderType = Type.GetType("Microsoft.Data.Tools.Schema.Tasks.Sql.TaskHostLoader,Microsoft.Data.Tools.Schema.Tasks.Sql", true);
            MethodInfo loadMethod = hostLoaderType.GetMethod("Load");
            Guard.IsNotNull(loadMethod, nameof(loadMethod), "Could find method 'Load' on 'TaskHostLoader'");

            // TaskLoggingHelper logger = new TaskLoggingHelper(task);
            // Since the TaskLoggingHelper exists in both Microsoft.Build.Utilities.Core or Microsoft.Build.Utilities.v4.0,
            // based on the MSBuild version, we have to inspect the target method parameter to specify it from the correct assembly.
            Type loggerType = loadMethod.GetParameters()[1].ParameterType;
            ParameterExpression loggerVariable = Expression.Variable(loggerType, "logger");
            ConstructorInfo loggerCtor = loggerType.GetConstructor(new[] { typeof(ITask) });
            Expression loggerValue = Expression.New(loggerCtor, taskParameter);
            Expression loggerAssign = Expression.Assign(loggerVariable, loggerValue);

            // TaskHostLoader hostLoader = new TaskHostLoader();
            ParameterExpression hostLoaderVariable = Expression.Variable(hostLoaderType, "hostLoader");
            Expression hostLoaderValue = Expression.New(hostLoaderType);
            Expression hostLoaderAssign = Expression.Assign(hostLoaderVariable, hostLoaderValue);

            // hostLoader.DatabaseSchemaProviderName = databaseSchemaProviderName;
            Expression databaseSchemaProviderNameProperty = Expression.Property(hostLoaderVariable, "DatabaseSchemaProviderName");
            Expression databaseSchemaProviderNameAssign = Expression.Assign(databaseSchemaProviderNameProperty, databaseSchemaProviderNameParameter);

            // hostLoader.ModelCollation = modelCollation;
            Expression modelCollationProperty = Expression.Property(hostLoaderVariable, "ModelCollation");
            Expression modelCollationAssign = Expression.Assign(modelCollationProperty, modelCollationParameter);

            // hostLoader.Source = source;
            Expression sourceProperty = Expression.Property(hostLoaderVariable, "Source");
            Expression sourceAssign = Expression.Assign(sourceProperty, sourceParameter);

            // hostLoader.SqlReferencePath = sqlReferencePath;
            Expression sqlReferencePathProperty = Expression.Property(hostLoaderVariable, "SqlReferencePath");
            Expression sqlReferencePathAssign = Expression.Assign(sqlReferencePathProperty, sqlReferencePathParameter);

            // hostLoader.Load(null, logger);
            Expression loadCall = Expression.Call(hostLoaderVariable, loadMethod, Expression.Constant(null, typeof(ITaskHost)), loggerVariable);

            //IEnumerator<DataSchemaError> errorEnumerator;
            //try
            //{
            //    errorEnumerator = hostLoader.LoadedErrorManager.GetAllErrors().GetEnumerator();
            //    while (errorEnumerator.MoveNext())
            //    {
            //        DataSchemaError error = errorEnumerator.Current;
            //        errorReporter.RegisterError(error.Document, error.Line, error.Column, error.ErrorCode, error.ErrorText);
            //    }
            //}
            //finally
            //{
            //    errorEnumerator.Dispose();
            //}
            Type dataSchemaErrorType = SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.DataSchemaError", true);
            ParameterExpression errorEnumeratorVariable = Expression.Variable(typeof(IEnumerator<>).MakeGenericType(dataSchemaErrorType), "errorEnumerator");

            Expression errorManagerProperty = Expression.Property(hostLoaderVariable, "LoadedErrorManager");
            Expression errorsCall = Expression.Call(errorManagerProperty, "GetAllErrors", new Type[0]);
            Expression errorEnumeratorValue = Expression.Call(errorsCall, typeof(IEnumerable<>).MakeGenericType(dataSchemaErrorType).GetMethod(nameof(IEnumerable<object>.GetEnumerator)));
            Expression errorEnumeratorAssign = Expression.Assign(errorEnumeratorVariable, errorEnumeratorValue);

            ParameterExpression errorVariable = Expression.Variable(dataSchemaErrorType, "error");
            Expression errorValue = Expression.Property(errorEnumeratorVariable, nameof(IEnumerator<object>.Current));
            Expression errorAssign = Expression.Assign(errorVariable, errorValue);

            Expression registerErrorCall = Expression.Call
            (
                errorReporterParameter
              , "RegisterError"
              , new Type[0]
              , Expression.Property(errorValue, "Document")
              , Expression.Property(errorValue, "Line")
              , Expression.Property(errorValue, "Column")
              , Expression.Call(Expression.Property(errorValue, "ErrorCode"), "ToString", new Type[0])
              , Expression.Property(errorValue, "Message")
            );
            Expression itemBlock = Expression.Block(new[] { errorVariable }, errorAssign, registerErrorCall);

            Expression propertyEnumeratorMoveNextCall = Expression.Call(errorEnumeratorVariable, typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext)));
            Expression loopCondition = Expression.Equal(propertyEnumeratorMoveNextCall, Expression.Constant(true));
            LabelTarget loopBreakLabel = Expression.Label("LoopBreak");
            Expression loopConditionBlock = Expression.IfThenElse(loopCondition, itemBlock, Expression.Break(loopBreakLabel));
            Expression loop = Expression.Loop(loopConditionBlock, loopBreakLabel);

            Expression tryBlock = Expression.Block(errorEnumeratorAssign, loop);
            Expression @finally = Expression.Call(errorEnumeratorVariable, typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose)));

            Expression tryFinally = Expression.TryFinally(tryBlock, @finally);

            // new TSqlModel(hostLoader.LoadedTaskHost.Model);
            Expression loadedTaskHostProperty = Expression.Property(hostLoaderVariable, "LoadedTaskHost");
            MemberExpression modelProperty = Expression.Property(loadedTaskHostProperty, "Model");
            Type dataSchemaModelType = SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.SchemaModel.DataSchemaModel", true);
            ConstructorInfo modelCtor = typeof(TSqlModel).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { dataSchemaModelType }, null);
            Expression modelValue = Expression.New(modelCtor, modelProperty);


            Expression block = Expression.Block
            (
                new[]
                {
                    loggerVariable
                  , hostLoaderVariable
                  , errorEnumeratorVariable
                }
              , loggerAssign
              , hostLoaderAssign
              , databaseSchemaProviderNameAssign
              , modelCollationAssign
              , sourceAssign
              , sqlReferencePathAssign
              , loadCall
              , tryFinally
              , modelValue
            );
            Expression<LoadPublicDataSchemaModel> lambda = Expression.Lambda<LoadPublicDataSchemaModel>
            (
                block
              , databaseSchemaProviderNameParameter
              , modelCollationParameter
              , sourceParameter
              , sqlReferencePathParameter
              , taskParameter
              , errorReporterParameter
            );
            LoadPublicDataSchemaModel compiled = lambda.Compile();
            return compiled;
        }

        private delegate TSqlModel LoadPublicDataSchemaModel(string databaseSchemaProviderName, string modelCollation, ITaskItem[] source, ITaskItem[] sqlReferencePath, ITask task, IErrorReporter errorReporter);
    }
}