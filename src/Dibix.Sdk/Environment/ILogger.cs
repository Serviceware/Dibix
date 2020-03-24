namespace Dibix.Sdk
{
    public interface ILogger
    {
        bool HasLoggedErrors { get; }

        void LogMessage(string text);
        void LogError(string code, string text, string source, int line, int column);
    }
}