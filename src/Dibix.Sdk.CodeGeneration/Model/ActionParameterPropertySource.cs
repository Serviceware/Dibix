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
        public string Converter { get; }
        public IReadOnlyCollection<ActionParameterPropertySourceNode> Nodes { get; }
        public IReadOnlyCollection<ActionParameterItemSource> ItemSources { get; }

        public ActionParameterPropertySource(ActionParameterSourceDefinition definition, string propertyName, string filePath, int line, int column, string converter, IList<ActionParameterPropertySourceNode> nodes, IList<ActionParameterItemSource> itemSources)
        {
            Definition = definition;
            PropertyName = propertyName;
            FilePath = filePath;
            Line = line;
            Column = column;
            Converter = converter;
            Nodes = new ReadOnlyCollection<ActionParameterPropertySourceNode>(nodes);
            ItemSources = new ReadOnlyCollection<ActionParameterItemSource>(itemSources);
        }
    }
}