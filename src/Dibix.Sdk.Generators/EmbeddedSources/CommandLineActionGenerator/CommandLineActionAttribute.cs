namespace Dibix.Sdk
{
    [global::System.AttributeUsage(global::System.AttributeTargets.Class)]
    internal sealed class CommandLineActionAttribute : global::System.Attribute
    {
        public string Name { get; }
        public string Description { get; }

        public CommandLineActionAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}