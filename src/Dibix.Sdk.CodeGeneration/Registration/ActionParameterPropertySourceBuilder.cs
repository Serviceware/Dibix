using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterPropertySourceBuilder : ActionParameterSourceBuilder
    {
        public ActionParameterSourceDefinition Definition { get; }
        public string PropertyName { get; }
        public string Converter { get; set; }
        public IList<ActionParameterPropertySourceNode> Nodes { get; }
        public ICollection<ActionParameterItemSourceBuilder> ItemSources { get; }
        public SourceLocation Location { get; }

        public ActionParameterPropertySourceBuilder(ActionParameterSourceDefinition definition, string propertyName, SourceLocation location)
        {
            Definition = definition;
            PropertyName = propertyName;
            Location = location;
            Nodes = new Collection<ActionParameterPropertySourceNode>();
            ItemSources = new Collection<ActionParameterItemSourceBuilder>();
        }

        public override ActionParameterSource Build(TypeReference type)
        {
            IList<ActionParameterItemSource> itemSources = ItemSources.Select(x => x.Build(type)).ToArray();
            ActionParameterPropertySource propertySource = new ActionParameterPropertySource(Definition, PropertyName, Location, Converter, Nodes, itemSources);
            return propertySource;
        }
    }
}