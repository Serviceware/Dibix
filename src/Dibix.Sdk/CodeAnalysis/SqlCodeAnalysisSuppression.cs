namespace Dibix.Sdk.CodeAnalysis
{
    public sealed class SqlCodeAnalysisSuppression
    {
        public string RuleName { get; }
        public string Key { get; }
        public string Hash { get; }

        public SqlCodeAnalysisSuppression(string ruleName, string key, string hash)
        {
            this.RuleName = ruleName;
            this.Key = key;
            this.Hash = hash;
        }
    }
}