using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ExplicitParameter
    {
        public string Name { get; }
        public ActionParameterSourceBuilder SourceBuilder { get; }
        public int Line { get; }
        public int Column { get; }

        public ExplicitParameter(JProperty property, ActionParameterSourceBuilder sourceBuilder)
        {
            SourceBuilder = sourceBuilder;
            Name = property.Name;
            IJsonLineInfo location = property.GetLineInfo();
            Line = location.LineNumber;
            Column = location.LinePosition;
        }
    }
}