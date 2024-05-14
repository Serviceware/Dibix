using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ParameterMapping : ExplicitParameter
    {
        public ActionParameterSourceBuilder SourceBuilder { get; }

        public ParameterMapping(JProperty property, ActionParameterSourceBuilder sourceBuilder) : base(property)
        {
            SourceBuilder = sourceBuilder;
        }
    }
}