using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class PathParameter
    {
        public string Name { get; }
        public int Index { get; }
        public bool Visited { get; set; }
        public SourceLocation Location { get; }

        public PathParameter(JProperty childRouteProperty, Group segment)
        {
            Name = segment.Value;
            Index = segment.Index;
            SourceLocation childRouteValueLocation = childRouteProperty.Value.GetSourceInfo();
            int matchIndex = segment.Index - 1;
            Location = new SourceLocation(childRouteValueLocation.Source, childRouteValueLocation.Line, childRouteValueLocation.Column + matchIndex);
        }
    }
}