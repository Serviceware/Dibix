using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterPropertySource : ActionParameterSource
    {
        public ActionParameterSourceDefinition Definition { get; }
        public string PropertyName { get; }
        public string Converter { get; }
        public IReadOnlyCollection<ActionParameterPropertySourceNode> Nodes { get; }
        public IReadOnlyCollection<ActionParameterItemSource> ItemSources { get; }
        public SourceLocation Location { get; }

        public ActionParameterPropertySource(ActionParameterSourceDefinition definition, string propertyName, SourceLocation location, string converter, IList<ActionParameterPropertySourceNode> nodes, IList<ActionParameterItemSource> itemSources)
        {
            Definition = definition;
            PropertyName = propertyName;
            Location = location;
            Converter = converter;
            Nodes = new ReadOnlyCollection<ActionParameterPropertySourceNode>(nodes);
            ItemSources = new ReadOnlyCollection<ActionParameterItemSource>(itemSources);
        }
    }
}