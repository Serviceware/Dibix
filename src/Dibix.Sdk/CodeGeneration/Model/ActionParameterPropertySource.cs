using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterPropertySource : ActionParameterSource
    {
        public ActionParameterSourceDefinition Definition { get; }
        public string PropertyName { get; }
        public string FilePath { get; }
        public int Line { get; }
        public int Column { get; }
        public string Converter { get; set; }
        public ICollection<ActionParameterPropertySourceNode> Nodes { get; }
        public ICollection<ActionParameterItemSource> ItemSources { get; }

        internal ActionParameterPropertySource(ActionParameterSourceDefinition definition, string propertyName, string filePath, int line, int column)
        {
            this.Definition = definition;
            this.PropertyName = propertyName;
            this.FilePath = filePath;
            this.Line = line;
            this.Column = column;
            this.Nodes = new Collection<ActionParameterPropertySourceNode>();
            this.ItemSources = new Collection<ActionParameterItemSource>();
        }
    }
}