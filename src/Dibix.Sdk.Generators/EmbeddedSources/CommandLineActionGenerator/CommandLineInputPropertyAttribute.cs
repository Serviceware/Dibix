#nullable enable
using System;

namespace Dibix.Sdk
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class CommandLineInputPropertyAttribute : Attribute
    {
        public string Name { get; }
        public CommandLineInputPropertyType Type { get; }
        public string? Category { get; set; }

        public CommandLineInputPropertyAttribute(string name, CommandLineInputPropertyType type)
        {
            Name = name;
            Type = type;
        }
    }
}
#nullable restore