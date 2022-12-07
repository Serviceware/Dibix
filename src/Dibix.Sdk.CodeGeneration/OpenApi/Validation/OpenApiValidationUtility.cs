using System.Text.RegularExpressions;
using Dibix.Sdk.Abstractions;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration.OpenApi
{
    internal static class OpenApiValidationUtility
    {
        private static readonly Regex ParseErrorPointerRegex = new Regex(@"(?<Root>#)|\/(?<Index>\d+)\/?|(?<Delimiter>\/)|(?<Escape>~1)", RegexOptions.Compiled);

        public static void LogError(OpenApiError error, string jsonFilePath, JToken json, ILogger logger)
        {
            if (!TryCollectErrorLocation(error, json, out string path, out int line, out int column)) 
                path = error.Pointer;

            logger.LogError($"{error.Message} ({path})", jsonFilePath, line, column);
        }

        private static bool TryCollectErrorLocation(OpenApiError error, JToken json, out string jsonPath, out int line, out int column)
        {
            jsonPath = ToJsonPath(error.Pointer);
            JToken token = json.SelectToken(jsonPath);
            if (token == null)
            {
                line = default;
                column = default;
                return false;
            }

            JsonSourceInfo sourceInfo = token.GetSourceInfo();
            line = sourceInfo.LineNumber;
            column = sourceInfo.LinePosition;
            return true;
        }

        private static string ToJsonPath(string openApiErrorPointer) => ParseErrorPointerRegex.Replace(openApiErrorPointer, x =>
        {
            if (x.Groups["Root"].Success)
                return "$";

            if (x.Groups["Index"].Success)
                return $".[{x.Groups["Index"].Value}].";

            if (x.Groups["Delimiter"].Success)
                return ".";

            if (x.Groups["Escape"].Success)
                return "/";

            return x.Value;
        });
    }
}