using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ControllerDefinition
    {
        public string Name { get; }
        public ISet<ActionDefinition> Actions { get; }
        public ICollection<string> ControllerImports { get; }

        public ControllerDefinition(string name)
        {
            this.Name = name;
            this.Actions = new HashSet<ActionDefinition>(FuncEqualityComparer<ActionDefinition>.Create(x => $"{x.Method}{x.ChildRoute}"));
            this.ControllerImports = new Collection<string>();
        }
    }
}