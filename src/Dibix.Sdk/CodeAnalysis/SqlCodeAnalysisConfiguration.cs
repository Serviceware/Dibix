namespace Dibix.Sdk.CodeAnalysis
{
    public sealed class SqlCodeAnalysisConfiguration
    {
        public string NamingConventionPrefix { get; set; }

        public SqlCodeAnalysisConfiguration() { }
        public SqlCodeAnalysisConfiguration(string namingConventionPrefix)
        {
            this.NamingConventionPrefix = namingConventionPrefix;
        }
    }
}