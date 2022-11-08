using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class PathParameter
    {
        public string Name { get; }
        public int Index { get; }
        public int Line { get; }
        public int Column { get; }

        public PathParameter(JProperty childRouteProperty, Group segment)
        {
            this.Name = segment.Value;
            this.Index = segment.Index;
            IJsonLineInfo childRouteValueLocation = childRouteProperty.Value.GetLineInfo();
            int matchIndex = segment.Index - 1;
            this.Line = childRouteValueLocation.LineNumber;
            this.Column = childRouteValueLocation.LinePosition + matchIndex;
        }
    }
}