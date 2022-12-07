using Dibix.Sdk.Abstractions;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeAnalysis
{
    public sealed class SqlCodeAnalysisUserConfigurationReader : IUserConfigurationReader
    {
        private readonly SqlCodeAnalysisConfiguration _configuration;

        public SqlCodeAnalysisUserConfigurationReader(SqlCodeAnalysisConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Read(JObject json)
        {
            const string sqlCodeAnalysisConfigurationName = "SqlCodeAnalysis";
            JObject sqlCodeAnalysisConfiguration = (JObject)json.Property(sqlCodeAnalysisConfigurationName)?.Value;
            if (sqlCodeAnalysisConfiguration == null)
                return;

            JProperty namingConventionPrefixProperty = sqlCodeAnalysisConfiguration.Property(nameof(SqlCodeAnalysisConfiguration.NamingConventionPrefix));
            if (namingConventionPrefixProperty != null)
            {
                _configuration.NamingConventionPrefix = (string)namingConventionPrefixProperty.Value;
            }
        }
    }
}