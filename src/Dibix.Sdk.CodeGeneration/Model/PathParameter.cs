using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class PathParameter
    {
        public string Name { get; }
        public int Index { get; }
        public string FilePath { get; set; }
        public int Line { get; }
        public int Column { get; }
        public bool Visited { get; set; }

        public PathParameter(JProperty childRouteProperty, Group segment)
        {
            Name = segment.Value;
            Index = segment.Index;
            JsonSourceInfo childRouteValueLocation = childRouteProperty.Value.GetSourceInfo();
            int matchIndex = segment.Index - 1;
            FilePath = childRouteValueLocation.FilePath;
            Line = childRouteValueLocation.LineNumber;
            Column = childRouteValueLocation.LinePosition + matchIndex;
        }
    }
}