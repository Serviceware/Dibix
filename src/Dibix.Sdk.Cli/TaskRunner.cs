using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.Cli
{
    internal static class TaskRunner
    {
        private static readonly Assembly SdkAssembly = typeof(Logger).Assembly;
        private static readonly Type[] ConstructorSignature =
        {
            typeof(ILogger),
            typeof(InputConfiguration)
        };
        private static readonly IDictionary<string, TaskRegistration> Tasks = CollectTasks().ToDictionary(x => x.Name);

        public static IEnumerable<string> RegisteredTaskRunnerNames => Tasks.Keys;

        public static bool Execute(string runnerName, string[] args, ILogger logger)
        {
            if (Tasks.TryGetValue(runnerName, out TaskRegistration registration))
            {
                InputConfiguration configuration = CollectInputConfiguration(registration, args);
                VisualStudioAwareLogger visualStudioAwareLogger = new VisualStudioAwareLogger(logger);
                visualStudioAwareLogger.BuildingInsideVisualStudio = configuration.GetSingleValue<bool>("BuildingInsideVisualStudio", throwOnInvalidKey: false);
                ITask task = registration.Factory(visualStudioAwareLogger, configuration);
                return task.Execute();
            }
            return false;
        }

        private static InputConfiguration CollectInputConfiguration(TaskRegistration task, IReadOnlyList<string> args)
        {
            if (!task.SupportsInputConfiguration || args.Count < 2)
                return InputConfiguration.Empty;

            string inputConfigurationFile = args[1];
            InputConfiguration configuration = InputConfiguration.Parse(inputConfigurationFile);
            return configuration;
        }

        private static IEnumerable<TaskRegistration> CollectTasks()
        {
            Type taskInterfaceType = typeof(ITask);
            foreach (Type type in SdkAssembly.GetTypes())
            {
                TaskAttribute taskAttribute = type.GetCustomAttribute<TaskAttribute>();
                if (taskAttribute == null)
                    continue;

                if (!taskInterfaceType.IsAssignableFrom(type))
                    throw new InvalidOperationException($"Type '{type}' is decorated with {nameof(TaskAttribute)}, but does not implement '{taskInterfaceType}'.");

                bool supportsInputConfiguration = type.GetCustomAttributes<TaskPropertyAttribute>().Any(x => x.Source == TaskPropertySource.Core);
                ConstructorInfo ctor = type.GetConstructorSafe(ConstructorSignature);
                
                ParameterExpression loggerParameter = Expression.Parameter(typeof(ILogger), "logger");
                ParameterExpression inputConfigurationParameter = Expression.Parameter(typeof(InputConfiguration), "inputConfiguration");
                Expression taskInstance = Expression.New(ctor, loggerParameter, inputConfigurationParameter);
                Expression<Func<ILogger, InputConfiguration, ITask>> lambda = Expression.Lambda<Func<ILogger, InputConfiguration, ITask>>(taskInstance, loggerParameter, inputConfigurationParameter);
                Func<ILogger, InputConfiguration, ITask> compiled = lambda.Compile();

                yield return new TaskRegistration(taskAttribute.Name, supportsInputConfiguration, compiled);
            }
        }

        private readonly struct TaskRegistration
        {
            public string Name { get; }
            public bool SupportsInputConfiguration { get; }
            public Func<ILogger, InputConfiguration, ITask> Factory { get; }

            public TaskRegistration(string name, bool supportsInputConfiguration, Func<ILogger, InputConfiguration, ITask> factory)
            {
                Name = name;
                SupportsInputConfiguration = supportsInputConfiguration;
                Factory = factory;
            }
        }

        // Unfortunately, VS doesn't parse the canonical error format correctly, leading to issues in the VS error list regarding error code and sub category.
        // This is not a fix nor a workaround. We just ignore the error code and add it to the text so it is more readable.
        // See: https://developercommunity.visualstudio.com/content/problem/98198/custom-error-code-is-not-shown-in-the-error-list.html
        private class VisualStudioAwareLogger : ILogger
        {
            private readonly ILogger _logger;

            public bool HasLoggedErrors => this._logger.HasLoggedErrors;
            public bool BuildingInsideVisualStudio { get; set; }

            public VisualStudioAwareLogger(ILogger logger) => this._logger = logger;

            public void LogMessage(string text) => this._logger.LogMessage(text);

            public void LogError(string text, string source, int? line, int? column) => LogError(code: null, text, source, line, column);
            public void LogError(string code, string text, string source, int? line, int? column)
            {
                this.AdjustParameters(ref code, ref text);
                this._logger.LogError(code, text, source, line, column);
            }
            public void LogError(string subCategory, string code, string text, string source, int? line, int? column)
            {
                this.AdjustParameters(ref code, ref text);
                this._logger.LogError(subCategory, code, text, source, line, column);
            }

            private void AdjustParameters(ref string code, ref string text)
            {
                if (!this.BuildingInsideVisualStudio) 
                    return;

                if (!String.IsNullOrEmpty(code))
                    text = $"{code}: {text}";

                code = null;
            }
        }
    }
}