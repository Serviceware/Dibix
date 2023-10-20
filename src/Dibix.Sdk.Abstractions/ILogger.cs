namespace Dibix.Sdk.Abstractions
{
    public interface ILogger
    {
        bool HasLoggedErrors { get; }

        void LogMessage(string text);
        void LogMessage(LogCategory category, string subCategory, string code, string text, string source, int? line, int? column);
    }
}