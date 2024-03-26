using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ControllerDefinition
    {
        public string Name { get; }
        public ICollection<ActionDefinition> Actions { get; }
        public ICollection<string> ControllerImports { get; }

        public ControllerDefinition(string name)
        {
            Name = name;
            Actions = new Collection<ActionDefinition>();
            ControllerImports = new Collection<string>();
        }
    }
}