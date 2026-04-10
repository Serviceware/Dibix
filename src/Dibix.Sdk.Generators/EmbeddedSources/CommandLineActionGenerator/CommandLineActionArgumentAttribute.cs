namespace Dibix.Sdk
{
    [global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true)]
    internal sealed class CommandLineActionArgumentAttribute : global::System.Attribute
    {
        public string Name { get; }
        public global::System.Type Type { get; }
        public string Description { get; }

        public CommandLineActionArgumentAttribute(string name, global::System.Type type, string description)
        {
            Name = name;
            Type = type;
            Description = description;
        }
    }
}