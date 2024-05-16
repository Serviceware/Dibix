using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class ExplicitParameter
    {
        public string Name { get; }
        public bool Visited { get; set; }
        public SourceLocation SourceLocation { get; }

        protected ExplicitParameter(JProperty property)
        {
            Name = property.Name;
            SourceLocation = property.GetSourceInfo();
        }
    }
}