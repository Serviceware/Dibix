namespace Dibix.Sdk.CodeAnalysis
{
    public sealed class SqlCodeAnalysisConfiguration
    {
        public string NamingConventionPrefix { get; }

        public SqlCodeAnalysisConfiguration(string namingConventionPrefix)
        {
            this.NamingConventionPrefix = namingConventionPrefix;
        }
    }
}