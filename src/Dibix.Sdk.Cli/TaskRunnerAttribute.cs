using System;

namespace Dibix.Sdk.Cli
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class TaskRunnerAttribute : Attribute
    {
        public string Name { get; }

        public TaskRunnerAttribute(string name) => this.Name = name;
    }
}