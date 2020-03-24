using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Data.Tools.Schema.Extensibility;
using Microsoft.Data.Tools.Schema.Tasks.Sql;
using Microsoft.Data.Tools.Schema.Utilities.Sql.Common;
using Microsoft.SqlServer.Dac.Model;
using Assembly = System.Reflection.Assembly;

namespace Dibix.Sdk.Sql
{
    internal static class PublicSqlDataSchemaModelLoader
    {
        #region Fields
        private static readonly Assembly SchemaSqlAssembly = typeof(IExtension).Assembly;
        private static readonly Assembly TasksAssembly = typeof(SqlBuildTask).Assembly;
        private static readonly Assembly UtilitiesAssembly = typeof(SqlConnectionInfoUtils).Assembly;
        private static readonly LoadPublicDataSchemaModel ModelFactory = CompileModelFactory();
        private static readonly ConcurrentDictionary<ILogger, ITask> TaskCache = new ConcurrentDictionary<ILogger, ITask>();
        #endregion

        #region Public Methods
        public static TSqlModel Load(string databaseSchemaProviderName, string modelCollation, IEnumerable<string> source, IEnumerable<string> sqlReferencePath, ILogger logger)
        {
            ITask task = TaskCache.GetOrAdd(logger, CreateTask);
            return ModelFactory(databaseSchemaProviderName, modelCollation, source.ToTaskItems(), sqlReferencePath.ToTaskItems(), task, logger);
        }
        #endregion

        #region Private Methods
        private static LoadPublicDataSchemaModel CompileModelFactory()
        {
            // (string databaseSchemaProviderName, ITaskItem[] source, ITaskItem[] sqlReferencePath, ITask task, ILogger logger) =>
            ParameterExpression databaseSchemaProviderNameParameter = Expression.Parameter(typeof(string), "databaseSchemaProviderName");
            ParameterExpression modelCollationParameter = Expression.Parameter(typeof(string), "modelCollation");
            ParameterExpression sourceParameter = Expression.Parameter(typeof(ITaskItem[]), "source");
            ParameterExpression sqlReferencePathParameter = Expression.Parameter(typeof(ITaskItem[]), "sqlReferencePath");
            ParameterExpression taskParameter = Expression.Parameter(typeof(ITask), "task");
            ParameterExpression loggerParameter = Expression.Parameter(typeof(ILogger), "logger");

            Type hostLoaderType = TasksAssembly.GetType("Microsoft.Data.Tools.Schema.Tasks.Sql.TaskHostLoader", true);
            MethodInfo loadMethod = hostLoaderType.GetMethod("Load");
            Guard.IsNotNull(loadMethod, nameof(loadMethod), "Could find method 'Load' on 'TaskHostLoader'");

            // Improve logging
            // SqlExceptionUtils._disableFiltering = true;
            // SqlExceptionUtils._initialized = true;
            Type sqlExceptionUtilsType = UtilitiesAssembly.GetType("Microsoft.Data.Tools.Schema.Utilities.Sql.Common.Exceptions.SqlExceptionUtils", true);
            FieldInfo disableFilteringField = TryGetStaticField(sqlExceptionUtilsType, "_disableFiltering");
            FieldInfo initializedField = TryGetStaticField(sqlExceptionUtilsType, "_initialized");
            Expression disableFilteringAssign = Expression.Assign(Expression.Field(null, disableFilteringField), Expression.Constant(true));
            Expression initializedAssign = Expression.Assign(Expression.Field(null, initializedField), Expression.Constant(true));

            // TaskLoggingHelper logger = new TaskLoggingHelper(task);
            Type loggerType = typeof(TaskLoggingHelper);
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
            //        logger.LogError(error.ErrorCode, error.ErrorText, error.Document, error.Line, error.Column);
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

            Expression logErrorCall = Expression.Call
            (
                loggerParameter
              , nameof(ILogger.LogError)
              , new Type[0]
              , Expression.Call(Expression.Property(errorValue, "ErrorCode"), "ToString", new Type[0])
              , Expression.Property(errorValue, "Message")
              , Expression.Property(errorValue, "Document")
              , Expression.Property(errorValue, "Line")
              , Expression.Property(errorValue, "Column")
            );
            Expression itemBlock = Expression.Block(new[] { errorVariable }, errorAssign, logErrorCall);

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
              , disableFilteringAssign
              , initializedAssign
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
              , loggerParameter
            );
            LoadPublicDataSchemaModel compiled = lambda.Compile();
            return compiled;
        }

        private static FieldInfo TryGetStaticField(Type type, string fieldName)
        {
            FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
            if (fieldInfo == null)
                throw new InvalidOperationException($"Invalid field {type}{fieldName}");

            return fieldInfo;
        }

        private static ITask CreateTask(ILogger logger) => new Task(logger);

        private static ITaskItem[] ToTaskItems(this IEnumerable<string> source) => source.Select(ToTaskItem).ToArray();

        private static ITaskItem ToTaskItem(string source) => new TaskItem(source);
        #endregion

        #region Delegates
        private delegate TSqlModel LoadPublicDataSchemaModel(string databaseSchemaProviderName, string modelCollation, ITaskItem[] source, ITaskItem[] sqlReferencePath, ITask task, ILogger logger);
        #endregion

        #region Nested types
        private sealed class BuildEngine : IBuildEngine
        {
            private readonly ILogger _logger;

            bool IBuildEngine.ContinueOnError => throw new NotSupportedException();
            int IBuildEngine.LineNumberOfTaskNode => throw new NotSupportedException();
            int IBuildEngine.ColumnNumberOfTaskNode => throw new NotSupportedException();
            string IBuildEngine.ProjectFileOfTaskNode => throw new NotSupportedException();

            public BuildEngine(ILogger logger) => this._logger = logger;

            void IBuildEngine.LogErrorEvent(BuildErrorEventArgs e) => this._logger.LogError(e.Code, e.Message, e.File, e.LineNumber, e.ColumnNumber);

            void IBuildEngine.LogWarningEvent(BuildWarningEventArgs e) => this._logger.LogError(e.Code, e.Message, e.File, e.LineNumber, e.ColumnNumber);

            void IBuildEngine.LogMessageEvent(BuildMessageEventArgs e) => this._logger.LogMessage(e.Message);

            void IBuildEngine.LogCustomEvent(CustomBuildEventArgs e) => throw new NotSupportedException();

            bool IBuildEngine.BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => throw new NotSupportedException();
        }

        private sealed class Task : ITask
        {
            private readonly BuildEngine _buildEngine;

            IBuildEngine ITask.BuildEngine
            {
                get => this._buildEngine;
                set => throw new NotSupportedException();
            }
            ITaskHost ITask.HostObject
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public Task(ILogger logger) => this._buildEngine = new BuildEngine(logger);

            bool ITask.Execute() => throw new NotSupportedException();
        }

        private sealed class TaskItem : ITaskItem
        {
            private readonly string _source;

            ICollection ITaskItem.MetadataNames { get; } = new object[0];
            int ITaskItem.MetadataCount => throw new NotSupportedException();
            string ITaskItem.ItemSpec
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public TaskItem(string source) => this._source = source;

            string ITaskItem.GetMetadata(string metadataName)
            {
                if (metadataName == "FullPath")
                    return this._source;

                throw new NotSupportedException();
            }

            void ITaskItem.SetMetadata(string metadataName, string metadataValue) => throw new NotSupportedException();

            void ITaskItem.RemoveMetadata(string metadataName) => throw new NotSupportedException();

            void ITaskItem.CopyMetadataTo(ITaskItem destinationItem) => throw new NotSupportedException();

            IDictionary ITaskItem.CloneCustomMetadata() => new Dictionary<string, string>();
        }
        #endregion
    }
}