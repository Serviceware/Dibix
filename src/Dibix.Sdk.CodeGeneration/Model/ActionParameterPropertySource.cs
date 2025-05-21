using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterPropertySource : ActionParameterSource, IActionParameterPropertySource, IActionParameterNestedPropertySource
    {
        public ActionParameterSourceDefinition Definition { get; }
        public string PropertyPath { get; }
        public string Converter { get; }
        public IReadOnlyCollection<ActionParameterPropertySourceNode> Nodes { get; }
        public IReadOnlyCollection<ActionParameterItemSource> ItemSources { get; }
        public SourceLocation Location { get; }
        public override TypeReference Type => Nodes.LastOrDefault()?.Property?.Type;

        public ActionParameterPropertySource(ActionParameterSourceDefinition definition, string propertyPath, SourceLocation location, string converter, IList<ActionParameterPropertySourceNode> nodes, IList<ActionParameterItemSource> itemSources)
        {
            Definition = definition;
            PropertyPath = propertyPath;
            Location = location;
            Converter = converter;
            Nodes = new ReadOnlyCollection<ActionParameterPropertySourceNode>(nodes);
            ItemSources = new ReadOnlyCollection<ActionParameterItemSource>(itemSources);
        }
    }
}