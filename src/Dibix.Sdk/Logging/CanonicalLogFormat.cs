﻿using System;
using System.Linq;

namespace Dibix.Sdk
{
    public static class CanonicalLogFormat
    {
        public static string ToWarningString(string subCategory, string code, string text, string source, int? line, int? column) => ToString(category: "warning", subCategory, code, text, source, line, column, endLine: null, endColumn: null);
        public static string ToErrorString(string subCategory, string code, string text, string source, int? line, int? column) => ToString(category: "error", subCategory, code, text, source, line, column, endLine: null, endColumn: null);
        private static string ToString(string category, string subCategory, string code, string text, string source, int? startLine, int? startColumn, int? endLine, int? endColumn)
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