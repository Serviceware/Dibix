using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dibix.Sdk.Cli
{
    internal abstract class TaskRunner
    {
        private static readonly IDictionary<string, Type> Runners;

        public static IEnumerable<string> RegisteredTaskRunnerNames => Runners.Keys;
        protected ILogger Logger { get; }

        static TaskRunner()
        {
            var query = from type in typeof(TaskRunner).Assembly.GetTypes()
                        let attrib = type.GetCustomAttribute<TaskRunnerAttribute>()
                        where attrib != null
                        select new KeyValuePair<string, Type>(attrib.Name, type);
            Runners = query.ToDictionary(x => x.Key, x => x.Value);
        }

        protected TaskRunner(ILogger logger) => this.Logger = logger;

        public static bool Execute(string runnerName, string inputConfigurationFile, ILogger logger)
        {
            if (Runners.TryGetValue(runnerName, out Type runnerType))
            {
                TaskRunner runner = (TaskRunner)Activator.CreateInstance(runnerType, logger);
                runner.Execute(inputConfigurationFile);
                return true;
            }
            return false;
        }

        protected abstract void Execute(InputConfiguration configuration);

        private void Execute(string inputConfigurationFile)
        {
            InputConfiguration configuration = InputConfiguration.Parse(inputConfigurationFile);
            this.Execute(configuration);
        }
    }
}