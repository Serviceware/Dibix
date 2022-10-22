using System;

namespace Dibix.Sdk.Abstractions
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class TaskPropertyAttribute : Attribute
    {
        public string Name { get; }
        public TaskPropertyType Type { get; }
        public TaskPropertySource Source { get; set; } = TaskPropertySource.Core;
        public string Category { get; set; }
        public string DefaultValue { get; set; }

        public TaskPropertyAttribute(string name, TaskPropertyType type)
        {
            this.Name = name;
            this.Type = type;
        }
    }
}