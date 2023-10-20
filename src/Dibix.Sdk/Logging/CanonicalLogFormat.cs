using System;
using System.Linq;

namespace Dibix.Sdk
{
    internal static class CanonicalLogFormat
    {
        public static string ToString(string category, string subCategory, string code, string text, string source, int? startLine, int? startColumn, int? endLine, int? endColumn)
        {
            if (!endLine.HasValue)
                endLine = startLine;

            if (!endColumn.HasValue)
                endColumn = startColumn;

            int?[] columns = { startLine, startColumn, endLine, endColumn };
            bool hasColumn = columns.Any(x => x.HasValue);
            string origin = source;
            if (origin != null && hasColumn)
                origin = $"{origin}({String.Join(",", columns.Where(x => x.HasValue))})";

            string[] parts = { subCategory, category, code };
            string merged = parts.Any() ? String.Join(" ", parts.Where(x => x != null)) : null;
            string[] segments = { origin, merged, text };
            string[] validSegments = segments.Where(x => x != null).ToArray();
            string result = String.Join(":", validSegments);
            if (validSegments.Length < 3)
                result = $"{result}:";

            return result;
        }
    }
}