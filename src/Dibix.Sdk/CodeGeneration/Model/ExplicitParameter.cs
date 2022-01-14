using Dibix.Sdk.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ExplicitParameter
    {
        public string Name { get; }
        public int Line { get; }
        public int Column { get; }
        public ActionParameterSource Source { get; }

        public ExplicitParameter(JProperty property, ActionParameterSource source)
        {
            this.Name = property.Name;
            IJsonLineInfo location = property.GetLineInfo();
            this.Line = location.LineNumber;
            this.Column = location.LinePosition;
            this.Source = source;
        }
    }
}