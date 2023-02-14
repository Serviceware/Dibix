using Dibix.Sdk.Abstractions;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ExplicitParameter
    {
        public string Name { get; }
        public ActionParameterSourceBuilder SourceBuilder { get; }
        public bool Visited { get; set; }
        public SourceLocation Location { get; }

        public ExplicitParameter(JProperty property, ActionParameterSourceBuilder sourceBuilder)
        {
            SourceBuilder = sourceBuilder;
            Name = property.Name;
            Location = property.GetSourceInfo();
        }
    }
}