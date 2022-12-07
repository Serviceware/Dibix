using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ExplicitParameter
    {
        public string Name { get; }
        public ActionParameterSourceBuilder SourceBuilder { get; }
        public string FilePath { get; set; }
        public int Line { get; }
        public int Column { get; }
        public bool Visited { get; set; }

        public ExplicitParameter(JProperty property, ActionParameterSourceBuilder sourceBuilder)
        {
            SourceBuilder = sourceBuilder;
            Name = property.Name;
            JsonSourceInfo location = property.GetSourceInfo();
            FilePath = location.FilePath;
            Line = location.LineNumber;
            Column = location.LinePosition;
        }
    }
}