namespace Dibix.Sdk.Abstractions
{
    public static class LoggerExtensions
    {
        public static void LogError(this ILogger logger, string text, SourceLocation location) => logger.LogError(text, location.Source, location.Line, location.Column);
    }
}