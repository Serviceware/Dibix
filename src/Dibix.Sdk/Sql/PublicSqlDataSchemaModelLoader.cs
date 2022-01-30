using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Data.Tools.Schema.Tasks.Sql;
using Microsoft.Data.Tools.Schema.Utilities.Sql.Common;
using Microsoft.SqlServer.Dac.Model;
using Assembly = System.Reflection.Assembly;

namespace Dibix.Sdk.Sql
{
    public static class PublicSqlDataSchemaModelLoader
    {
        #region Fields
        private static readonly Assembly TasksAssembly = typeof(SqlBuildTask).Assembly;
        private static readonly Assembly UtilitiesAssembly = typeof(SqlConnectionInfoUtils).Assembly;
        private static readonly LoadPublicDataSchemaModel ModelFactory = CompileModelFactory();
        private static readonly ConcurrentDictionary<ILogger, ITask> TaskCache = new ConcurrentDictionary<ILogger, ITask>();
        #endregion

        #region Public Methods
        public static TSqlModel Load(string projectName, string databaseSchemaProviderName, string modelCollation, IEnumerable<TaskItem> source, ICollection<TaskItem> sqlReferencePath, ILogger logger)
        {
            RestrictEmbeddedReferences(projectName, sqlReferencePath, logger);
            ITask task = TaskCache.GetOrAdd(logger, CreateTask);
            return ModelFactory(databaseSchemaProviderName, modelCollation, source.ToMSBuildTaskItems(), sqlReferencePath.ToMSBuildTaskItems(), task, logger);
        }
        #endregion

        #region Private Methods
        private static void RestrictEmbeddedReferences(string projectName, IEnumerable<TaskItem> sqlReferencePath, ILogger logger)
        {
            foreach (TaskItem reference in sqlReferencePath)
            {
                string path = reference.GetFullPath();
                string fileName = Path.GetFileNameWithoutExtension(path);

                if (reference.TryGetValue("SuppressMissingDependenciesErrors", out string suppressionValue) && Boolean.Parse(suppressionValue))
                {
                    logger.LogError(null, $"SuppressMissingDependenciesErrors is not supported: {path}", null, default, default);
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
                    logger.LogError(null, $"Unsupported package reference: {path}", null, default, default);
                }
                else if (isEmbedded.Value)
                {
                    // TODO: Dirty suppression..
                    if (projectName == "Helpline.DML")
                        continue;
                    
                    logger.LogError(null, $"Unsupported reference to DML package: {path}", null, default, default);
                }
            }
        }

        private static LoadPublicDataSchemaModel CompileModelFactory()
        {
            // IEnumerator<DataSchemaError> errorEnumerator;
            // SqlExceptionUtils._disableFiltering = true;
            // SqlExceptionUtils._initialized = true;
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
            // return new TSqlModel(hostLoader.LoadedTaskHost.Model);

            // (string databaseSchemaProviderName, string modelCollation, ITaskItem[] source, ITaskItem[] sqlReferencePath, ITask task, ILogger logger) =>
            ParameterExpression databaseSchemaProviderNameParameter = Expression.Parameter(typeof(string), "databaseSchemaProviderName");
            ParameterExpression modelCollationParameter = Expression.Parameter(typeof(string), "modelCollation");
            ParameterExpression sourceParameter = Expression.Parameter(typeof(ITaskItem[]), "source");
            ParameterExpression sqlReferencePathParameter = Expression.Parameter(typeof(ITaskItem[]), "sqlReferencePath");
            ParameterExpression taskParameter = Expression.Parameter(typeof(ITask), "task");
            ParameterExpression loggerParameter = Expression.Parameter(typeof(ILogger), "logger");

            // Improve logging
            // SqlExceptionUtils._disableFiltering = true;
            // SqlExceptionUtils._initialized = true;
            Type sqlExceptionUtilsType = UtilitiesAssembly.GetType("Microsoft.Data.Tools.Schema.Utilities.Sql.Common.Exceptions.SqlExceptionUtils", true);
            FieldInfo disableFilteringField = TryGetStaticField(sqlExceptionUtilsType, "_disableFiltering");
            FieldInfo initializedField = TryGetStaticField(sqlExceptionUtilsType, "_initialized");
            Expression disableFilteringAssign = Expression.Assign(Expression.Field(null, disableFilteringField), Expression.Constant(true));
            Expression initializedAssign = Expression.Assign(Expression.Field(null, initializedField), Expression.Constant(true));

            // TaskLoggingHelper loggingHelper = new TaskLoggingHelper(task);
            Type loggingHelperType = typeof(TaskLoggingHelper);
            ParameterExpression loggingHelperVariable = Expression.Variable(loggingHelperType, "loggingHelper");
            ConstructorInfo loggingHelperCtor = loggingHelperType.GetConstructor(new[] { typeof(ITask) });
            Expression loggingHelperValue = Expression.New(loggingHelperCtor, taskParameter);
            Expression loggingHelperAssign = Expression.Assign(loggingHelperVariable, loggingHelperValue);

            // TaskHostLoader hostLoader = new TaskHostLoader();
            Type hostLoaderType = TasksAssembly.GetType("Microsoft.Data.Tools.Schema.Tasks.Sql.TaskHostLoader", true);
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
            Type dataSchemaErrorType = DacReflectionUtility.SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.DataSchemaError", true);
            ExpressionUtility.Foreach
            (
                name: "error"
              , enumerable: errorsCall
              , elementType: dataSchemaErrorType
              , bodyBuilder: builder => CompileErrorIterator(builder, loggerParameter)
              , enumeratorVariable: out ParameterExpression enumeratorVariable
              , enumeratorStatement: out Expression enumeratorStatement
            );

            // new TSqlModel(hostLoader.LoadedTaskHost.Model);
            Expression loadedTaskHostProperty = Expression.Property(hostLoaderVariable, "LoadedTaskHost");
            MemberExpression modelProperty = Expression.Property(loadedTaskHostProperty, "Model");
            Type dataSchemaModelType = DacReflectionUtility.SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.SchemaModel.DataSchemaModel", true);
            ConstructorInfo modelCtor = typeof(TSqlModel).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { dataSchemaModelType }, null);
            Expression modelValue = Expression.New(modelCtor, modelProperty);


            Expression block = Expression.Block
            (
                new[]
                {
                    loggingHelperVariable
                  , hostLoaderVariable
                  , enumeratorVariable
                }
              , disableFilteringAssign
              , initializedAssign
              , loggingHelperAssign
              , hostLoaderAssign
              , databaseSchemaProviderNameAssign
              , modelCollationAssign
              , sourceAssign
              , sqlReferencePathAssign
              , loadCall
              , enumeratorStatement
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

        private static void CompileErrorIterator(IForeachBodyBuilder builder, Expression loggerParameter)
        {
            // logger.LogError(error.ErrorCode.ToString(), error.Message, error.Document, error.Line, error.Column);
            Expression logErrorCall = Expression.Call
            (
                loggerParameter
              , nameof(ILogger.LogError)
              , Type.EmptyTypes
              , Expression.Call(Expression.Property(builder.Element, "ErrorCode"), "ToString", Type.EmptyTypes)
              , Expression.Property(builder.Element, "Message")
              , Expression.Property(builder.Element, "Document")
              , Expression.Convert(Expression.Property(builder.Element, "Line"), typeof(int?))
              , Expression.Convert(Expression.Property(builder.Element, "Column"), typeof(int?))
            );
            builder.AddStatement(logErrorCall);
        }

        private static FieldInfo TryGetStaticField(Type type, string fieldName)
        {
            FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
            if (fieldInfo == null)
                throw new InvalidOperationException($"Invalid field {type}{fieldName}");

            return fieldInfo;
        }

        private static ITask CreateTask(ILogger logger) => new Task(logger);

        private static ITaskItem[] ToMSBuildTaskItems(this IEnumerable<TaskItem> source) => source.Select(ToMSBuildTaskItem).ToArray();

        private static ITaskItem ToMSBuildTaskItem(TaskItem source) => new TaskItemWrapper(source);
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

            string ITaskItem.GetMetadata(string metadataName) => this._item[metadataName];

            void ITaskItem.SetMetadata(string metadataName, string metadataValue) => throw new NotSupportedException();

            void ITaskItem.RemoveMetadata(string metadataName) => throw new NotSupportedException();

            void ITaskItem.CopyMetadataTo(ITaskItem destinationItem) => throw new NotSupportedException();

            IDictionary ITaskItem.CloneCustomMetadata() => this._item;
        }
        #endregion
    }
}