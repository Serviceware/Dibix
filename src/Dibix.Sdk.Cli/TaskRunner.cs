using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dibix.Sdk.Cli
{
    internal abstract class TaskRunner
    {
        private static readonly IDictionary<string, Type> Runners;
        private readonly VisualStudioAwareLogger _logger;

        public static IEnumerable<string> RegisteredTaskRunnerNames => Runners.Keys;
        protected ILogger Logger => this._logger;
        protected bool BuildingInsideVisualStudio
        {
            get => this._logger.BuildingInsideVisualStudio;
            set => this._logger.BuildingInsideVisualStudio = value;
        }

        static TaskRunner()
        {
            var query = from type in typeof(TaskRunner).Assembly.GetTypes()
                        let attrib = type.GetCustomAttribute<TaskRunnerAttribute>()
                        where attrib != null
                        select new KeyValuePair<string, Type>(attrib.Name, type);
            Runners = query.ToDictionary(x => x.Key, x => x.Value);
        }

        protected TaskRunner(ILogger logger) => this._logger = new VisualStudioAwareLogger(logger);

        public static bool Execute(string runnerName, string[] args, ILogger logger)
        {
            if (Runners.TryGetValue(runnerName, out Type runnerType))
            {
                TaskRunner runner = (TaskRunner)Activator.CreateInstance(runnerType, logger);
                return runner.Execute(args);
            }
            return false;
        }

        protected abstract bool Execute(string[] args);

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