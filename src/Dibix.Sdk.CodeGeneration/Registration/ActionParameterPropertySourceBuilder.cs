using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterPropertySourceBuilder : ActionParameterSourceBuilder
    {
        public ActionParameterSourceDefinition Definition { get; }
        public string PropertyName { get; }
        public string FilePath { get; }
        public int Line { get; }
        public int Column { get; }
        public string Converter { get; set; }
        public IList<ActionParameterPropertySourceNode> Nodes { get; }
        public ICollection<ActionParameterItemSourceBuilder> ItemSources { get; }

        public ActionParameterPropertySourceBuilder(ActionParameterSourceDefinition definition, string propertyName, string filePath, int line, int column)
        {
            Definition = definition;
            PropertyName = propertyName;
            FilePath = filePath;
            Line = line;
            Column = column;
            Nodes = new Collection<ActionParameterPropertySourceNode>();
            ItemSources = new Collection<ActionParameterItemSourceBuilder>();
        }

        public override ActionParameterSource Build(TypeReference type)
        {
            IList<ActionParameterItemSource> itemSources = ItemSources.Select(x => x.Build(type)).ToArray();
            ActionParameterPropertySource propertySource = new ActionParameterPropertySource(Definition, PropertyName, FilePath, Line, Column, Converter, Nodes, itemSources);
            return propertySource;
        }
    }
}