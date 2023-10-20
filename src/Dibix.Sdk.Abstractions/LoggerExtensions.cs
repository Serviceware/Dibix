namespace Dibix.Sdk.Abstractions
{
    public static class LoggerExtensions
    {
        public static void LogError(this ILogger logger, string text, SourceLocation location) => logger.LogMessage(LogCategory.Error, subCategory: null, code: null, text, location.Source, location.Line, location.Column);
        public static void LogError(this ILogger logger, string text, string source, int line, int column) => logger.LogMessage(LogCategory.Error, subCategory: null, code: null, text, source, line, column);
        public static void LogError(this ILogger logger, string code, string text, string source, int line, int column) => logger.LogMessage(LogCategory.Error, subCategory: null, code, text, source, line, column);
        public static void LogError(this ILogger logger, string code, string subCategory, string text, string source, int line, int column) => logger.LogMessage(LogCategory.Error, subCategory, code, text, source, line, column);

        public static void LogWarning(this ILogger logger, string text, SourceLocation location) => logger.LogMessage(LogCategory.Warning, subCategory: null, code: null, text, location.Source, location.Line, location.Column);
    }
}