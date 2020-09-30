namespace Dibix.Sdk.CodeAnalysis
{
    public interface ISqlCodeAnalysisSuppressionService
    {
        bool IsSuppressed(string ruleName, string key, string hash);
        void ResetSuppressions();
    }
}