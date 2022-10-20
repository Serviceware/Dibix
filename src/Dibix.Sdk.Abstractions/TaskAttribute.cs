using System;

namespace Dibix.Sdk.Abstractions
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TaskAttribute : Attribute
    {
        public string Name { get; }

        public TaskAttribute(string name) => this.Name = name;
    }
}