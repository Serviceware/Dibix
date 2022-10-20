namespace Dibix.Generators
{
    [global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true)]
    internal sealed class TaskPropertyAttribute : global::System.Attribute
    {
        public string Name { get; }
        public global::Dibix.Generators.TaskPropertyType Type { get; }
        public global::Dibix.Generators.TaskPropertySource Source { get; set; } = global::Dibix.Generators.TaskPropertySource.Core;
        public string Category { get; set; }
        public string DefaultValue { get; set; }

#pragma warning disable CS8618
        public TaskPropertyAttribute(string name, global::Dibix.Generators.TaskPropertyType type)
#pragma warning restore CS8618
        {
            this.Name = name;
            this.Type = type;
        }
    }
}