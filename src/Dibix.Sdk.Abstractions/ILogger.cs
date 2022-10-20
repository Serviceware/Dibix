namespace Dibix.Sdk.Abstractions
{
    public interface ILogger
    {
        bool HasLoggedErrors { get; }

        void LogMessage(string text);
        void LogError(string text, string source, int? line, int? column);
        void LogError(string code, string text, string source, int? line, int? column);
        void LogError(string subCategory, string code, string text, string source, int? line, int? column);
    }
}