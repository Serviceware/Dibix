using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dibix.Sdk.Abstractions;
using Microsoft.Build.Framework;
using Microsoft.Data.Tools.Schema.Tasks.Sql;
using Microsoft.SqlServer.Dac.Model;
using Assembly = System.Reflection.Assembly;
using ILogger = Dibix.Sdk.Abstractions.ILogger;
using ITask = Microsoft.Build.Framework.ITask;
using TaskItem = Dibix.Sdk.Abstractions.TaskItem;

namespace Dibix.Sdk.Sql
{
    public static class PublicSqlDataSchemaModelLoader
    {
        #region Fields
        private static readonly Assembly TasksAssembly = typeof(SqlBuildTask).Assembly;
        private static readonly LoadPublicDataSchemaModel ModelFactory = CompileModelFactory();
        private static readonly ConcurrentDictionary<ILogger, ITask> TaskCache = new ConcurrentDictionary<ILogger, ITask>();
        #endregion

        #region Public Methods
        public static PublicSqlDataSchemaModel Load(bool preventDmlReferences, string databaseSchemaProviderName, string modelCollation, IEnumerable<TaskItem> source, ICollection<TaskItem> sqlReferencePath, ILogger logger)
        {
            NullableColumnSchemaAnalyzerProxy.Setup();
            ValidateReferences(preventDmlReferences, sqlReferencePath, logger);
            ITask task = TaskCache.GetOrAdd(logger, CreateTask);
            return ModelFactory(databaseSchemaProviderName, modelCollation, source.ToMSBuildTaskItems(), sqlReferencePath.ToMSBuildTaskItems(), task, logger);
        }
        #endregion

        #region Private Methods
        private static void ValidateReferences(bool preventDmlReferences, IEnumerable<TaskItem> sqlReferencePath, ILogger logger)
        {
            foreach (TaskItem reference in sqlReferencePath)
            {
                string path = reference.GetFullPath();
                string fileName = Path.GetFileNameWithoutExtension(path);

                if (reference.TryGetValue("SuppressMissingDependenciesErrors", out string suppressionValue) && suppressionValue.Length > 0 && Boolean.Parse(suppressionValue))
                {
                    logger.LogError($"SuppressMissingDependenciesErrors is not supported: {path}", null, default, default);
                    continue;
                }

                if (Path.GetExtension(path) != ".dacpac")
                    continue;

                switch (fileName)
                {
                    case "master":
                    case "msdb":
                        continue;
                }

                bool? isEmbedded = DacMetadataManager.IsEmbedded(path);
                if (!isEmbedded.HasValue)
                {
                    logger.LogError($"Unsupported package reference: {path}", null, default, default);
                }
                else if (isEmbedded.Value)
                {
                    if (!preventDmlReferences)
                        continue;
                    
                    logger.LogError($"Unsupported reference to DML package: {path}", null, default, default);
                }
            }
        }

        private static LoadPublicDataSchemaModel CompileModelFactory()
        {
            // IEnumerator<DataSchemaError> errorEnumerator;
            // TaskLoggingHelper loggingHelper = new TaskLoggingHelper(task);
            // TaskHostLoader hostLoader = new TaskHostLoader();
            // hostLoader.DatabaseSchemaProviderName = databaseSchemaProviderName;
            // hostLoader.ModelCollation = modelCollation;
            // hostLoader.Source = source;
            // hostLoader.SqlReferencePath = sqlReferencePath;
            // hostLoader.Load(null, loggingHelper);
            // try
            // {
            //     errorEnumerator = hostLoader.LoadedErrorManager.GetAllErrors().GetEnumerator();
            //     while (errorEnumerator.MoveNext())
            //     {
            //         DataSchemaError errorElement = errorEnumerator.Current;
            //         logger.LogError(errorElement.ErrorCode.ToString(), errorElement.Message, errorElement.Document, errorElement.Line, errorElement.Column);
            //     }
            // }
            // finally
            // {
            //     if (errorEnumerator != null)
            //         errorEnumerator.Dispose();
            // }
            // TSqlModel model = TSqlModel(hostLoader.LoadedTaskHost.Model);
            // return new PublicSqlDataSchemaModel(model, new [] { hostLoader, hostLoader.LoadedTaskHost, hostLoader.LoadedErrorManager });

            // (string databaseSchemaProviderName, string modelCollation, ITaskItem[] source, ITaskItem[] sqlReferencePath, ITask task, ILogger logger) =>
            ParameterExpression databaseSchemaProviderNameParameter = Expression.Parameter(typeof(string), "databaseSchemaProviderName");
            ParameterExpression modelCollationParameter = Expression.Parameter(typeof(string), "modelCollation");
            ParameterExpression sourceParameter = Expression.Parameter(typeof(ITaskItem[]), "source");
            ParameterExpression sqlReferencePathParameter = Expression.Parameter(typeof(ITaskItem[]), "sqlReferencePath");
            ParameterExpression taskParameter = Expression.Parameter(typeof(ITask), "task");
            ParameterExpression loggerParameter = Expression.Parameter(typeof(ILogger), "logger");

            // TaskLoggingHelper loggingHelper = new TaskLoggingHelper(task);
#if NETFRAMEWORK
            Assembly buildUtilitiesAssembly = Assembly.Load("Microsoft.Build.Utilities.v4.0, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
#else
            Assembly buildUtilitiesAssembly = typeof(Microsoft.Build.Utilities.TaskLoggingHelper).Assembly;
#endif
            Type loggingHelperType = buildUtilitiesAssembly.GetType("Microsoft.Build.Utilities.TaskLoggingHelper", throwOnError: true);
            ParameterExpression loggingHelperVariable = Expression.Variable(loggingHelperType, "loggingHelper");
            ConstructorInfo loggingHelperCtor = loggingHelperType.GetConstructorSafe(typeof(ITask));
            Expression loggingHelperValue = Expression.New(loggingHelperCtor, taskParameter);
            Expression loggingHelperAssign = Expression.Assign(loggingHelperVariable, loggingHelperValue);

            // TaskHostLoader hostLoader = new TaskHostLoader();
            Type hostLoaderType = TasksAssembly.GetType("Microsoft.Data.Tools.Schema.Tasks.Sql.TaskHostLoader", throwOnError: true);
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

            // hostLoader.Load(null, loggingHelper);
            Expression loadCall = Expression.Call(hostLoaderVariable, "Load", Type.EmptyTypes, Expression.Constant(null, typeof(ITaskHost)), loggingHelperVariable);

            // while (errorEnumerator.MoveNext())
            // {
            //     DataSchemaError errorElement = errorEnumerator.Current;
            //     logger.LogError(error.ErrorCode.ToString(), error.Message, error.Document, error.Line, error.Column);
            // }
            Expression errorManagerProperty = Expression.Property(hostLoaderVariable, "LoadedErrorManager");
            Expression errorsCall = Expression.Call(errorManagerProperty, "GetAllErrors", Type.EmptyTypes);
            Type dataSchemaErrorType = DacReflectionUtility.SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.DataSchemaError", throwOnError: true);
            ExpressionUtility.Foreach
            (
                name: "error"
              , enumerable: errorsCall
              , elementType: dataSchemaErrorType
              , bodyBuilder: builder => CompileErrorIterator(builder, loggerParameter)
              , enumeratorVariable: out ParameterExpression enumeratorVariable
              , enumeratorStatement: out Expression enumeratorStatement
            );

            // TSqlModel model = new TSqlModel(hostLoader.LoadedTaskHost.Model);
            Expression loadedTaskHostProperty = Expression.Property(hostLoaderVariable, "LoadedTaskHost");
            MemberExpression modelProperty = Expression.Property(loadedTaskHostProperty, "Model");
            Type dataSchemaModelType = DacReflectionUtility.SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.SchemaModel.DataSchemaModel", throwOnError: true);
            ConstructorInfo modelCtor = typeof(TSqlModel).GetConstructorSafe(BindingFlags.NonPublic | BindingFlags.Instance, dataSchemaModelType);
            ParameterExpression modelVariable = Expression.Variable(typeof(TSqlModel), "model");
            Expression modelValue = Expression.New(modelCtor, modelProperty);
            Expression modelAssign = Expression.Assign(modelVariable, modelValue);

            // return new PublicSqlDataSchemaModel(model, disposeHostLoader);
            ConstructorInfo publicSqlDataSchemaModelCtor = typeof(PublicSqlDataSchemaModel).GetConstructorSafe(typeof(TSqlModel), typeof(object), typeof(Action<object>));
            Action<object> disposeHostLoaderCall = CompileDisposeHostLoader(hostLoaderType);
            Expression disposeHostLoaderValue = Expression.Constant(disposeHostLoaderCall, typeof(Action<object>));
            Expression publicSqlDataSchemaModel = Expression.New(publicSqlDataSchemaModelCtor, modelVariable, hostLoaderVariable, disposeHostLoaderValue);


            Expression block = Expression.Block
            (
                [
                    loggingHelperVariable
                  , hostLoaderVariable
                  , enumeratorVariable
                  , modelVariable
                ]
              , loggingHelperAssign
              , hostLoaderAssign
              , databaseSchemaProviderNameAssign
              , modelCollationAssign
              , sourceAssign
              , sqlReferencePathAssign
              , loadCall
              , enumeratorStatement
              , modelAssign
              , publicSqlDataSchemaModel
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

        private static void CompileErrorIterator(IForeachBodyBuilder builder, Expression loggerParameter)
        {
            // PublicSqlDataSchemaModelLoader.LogError(logger, error.ErrorCode.ToString(), error.Message, error.Document, error.Line, error.Column);
            Expression logErrorCall = Expression.Call
            (
                typeof(PublicSqlDataSchemaModelLoader)
              , nameof(LogError)
              , Type.EmptyTypes
              , loggerParameter
              , Expression.Call(Expression.Property(builder.Element, "ErrorCode"), "ToString", Type.EmptyTypes)
              , Expression.Property(builder.Element, "Message")
              , Expression.Property(builder.Element, "Document")
              , Expression.Convert(Expression.Property(builder.Element, "Line"), typeof(int?))
              , Expression.Convert(Expression.Property(builder.Element, "Column"), typeof(int?))
            );
            builder.AddStatement(logErrorCall);
        }

        private static Action<object> CompileDisposeHostLoader(Type hostLoaderType)
        {
            // Action<object> disposeHostLoader = hostLoader => 
            // {
            //     ((TaskHostLoader)hostLoader).Dispose();
            //     ((TaskHostLoader)hostLoader)._loadedTaskHost.Dispose();
            //     ((TaskHostLoader)hostLoader)._loadedErrorManager.Dispose();
            //     ((TaskHostLoader)hostLoader)._loadedTaskHost = null;
            //     ((TaskHostLoader)hostLoader)._loadedErrorManager = null;
            // };
            ParameterExpression hostLoaderParameter = Expression.Parameter(typeof(object), "hostLoader");
            Expression hostLoaderVariable = Expression.Convert(hostLoaderParameter, hostLoaderType);
            Expression disposeHostLoader = Expression.Call(hostLoaderVariable, nameof(IDisposable.Dispose), []);
            Expression loadedTaskHostField = Expression.Field(hostLoaderVariable, "_loadedTaskHost");
            Expression loadedErrorManagerField = Expression.Field(hostLoaderVariable, "_loadedErrorManager");
            Expression loadedTaskHostDispose = Expression.Call(loadedTaskHostField, nameof(IDisposable.Dispose), []);
            Expression loadedErrorManagerDispose = Expression.Call(loadedErrorManagerField, nameof(IDisposable.Dispose), []);
            Expression loadedTaskHostAssign = Expression.Assign(loadedTaskHostField, Expression.Constant(null, loadedTaskHostField.Type));
            Expression loadedErrorManagerAssign = Expression.Assign(loadedErrorManagerField, Expression.Constant(null, loadedErrorManagerField.Type));
            Expression disposeHostLoaderBlock = Expression.Block
            (
                disposeHostLoader
              , loadedTaskHostDispose
              , loadedErrorManagerDispose
              , loadedTaskHostAssign
              , loadedErrorManagerAssign
            );
            Expression<Action<object>> lambda = Expression.Lambda<Action<object>>(disposeHostLoaderBlock, hostLoaderParameter);
            Action<object> compiled = lambda.Compile();
            return compiled;
        }

        private static FieldInfo TryGetStaticField(IReflect type, string fieldName)
        {
            FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
            if (fieldInfo == null)
                throw new InvalidOperationException($"Invalid field {type}{fieldName}");

            return fieldInfo;
        }

        private static ITask CreateTask(ILogger logger) => new Task(logger);

        private static ITaskItem[] ToMSBuildTaskItems(this IEnumerable<TaskItem> source) => source.Select(ToMSBuildTaskItem).ToArray();

        private static ITaskItem ToMSBuildTaskItem(TaskItem source) => new TaskItemWrapper(source);

        private static void LogError(ILogger logger, string errorCode, string message, string document, int? line, int? column) => logger.LogError(errorCode, message, document, line ?? default, column ?? default); 
#endregion

        #region Delegates
        private delegate PublicSqlDataSchemaModel LoadPublicDataSchemaModel(string databaseSchemaProviderName, string modelCollation, ITaskItem[] source, ITaskItem[] sqlReferencePath, ITask task, ILogger logger);
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

            void IBuildEngine.LogErrorEvent(BuildErrorEventArgs e) => this._logger.LogError(e.Message, e.File, e.LineNumber, e.ColumnNumber);

            void IBuildEngine.LogWarningEvent(BuildWarningEventArgs e) => this._logger.LogError(e.Message, e.File, e.LineNumber, e.ColumnNumber);

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

        private sealed class TaskItemWrapper : ITaskItem
        {
            private readonly TaskItem _item;

            ICollection ITaskItem.MetadataNames { get; } = Array.Empty<object>();
            int ITaskItem.MetadataCount => throw new NotSupportedException();
            string ITaskItem.ItemSpec
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public TaskItemWrapper(TaskItem item) => this._item = item;

            string ITaskItem.GetMetadata(string metadataName)
            {
                if (!this._item.TryGetValue(metadataName, out string value))
                    throw new KeyNotFoundException($"The item does not contain metadata with name '{metadataName}'");

                return value;
            }

            void ITaskItem.SetMetadata(string metadataName, string metadataValue) => throw new NotSupportedException();

            void ITaskItem.RemoveMetadata(string metadataName) => throw new NotSupportedException();

            void ITaskItem.CopyMetadataTo(ITaskItem destinationItem) => throw new NotSupportedException();

            IDictionary ITaskItem.CloneCustomMetadata() => this._item;
        }
        #endregion
    }
}