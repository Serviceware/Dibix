using System;
using System.IO;
using System.Linq;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Sdk.CodeGeneration;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk
{
    internal sealed class SqlCoreTaskConfiguration
    {
        public SqlCodeAnalysisConfiguration SqlCodeAnalysis { get; } = new SqlCodeAnalysisConfiguration();
        public EndpointConfiguration Endpoints { get; } = new EndpointConfiguration();

        private SqlCoreTaskConfiguration() { }

        public static SqlCoreTaskConfiguration Create(string filePath, IFileSystemProvider fileSystemProvider, ILogger logger)
        {
            SqlCoreTaskConfiguration configuration = new SqlCoreTaskConfiguration();
            configuration.Endpoints.AppendBuiltInParameterSources();
            if (!File.Exists(filePath))
            {
                return configuration;
            }

            SqlCoreTaskConfigurationReader configurationReader = new SqlCoreTaskConfigurationReader(filePath, configuration, fileSystemProvider, logger);
            configurationReader.Collect();
            return configuration;
        }

        private sealed class SqlCoreTaskConfigurationReader : JsonSchemaDefinitionReader
        {
            private readonly string _filePath;
            private readonly SqlCoreTaskConfiguration _configuration;

            public SqlCoreTaskConfigurationReader(string filePath, SqlCoreTaskConfiguration configuration, IFileSystemProvider fileSystemProvider, ILogger logger) : base(fileSystemProvider, logger)
            {
                this._filePath = filePath;
                this._configuration = configuration;
            }

            public void Collect() => base.Collect(Enumerable.Repeat(this._filePath, 1));

            protected override string SchemaName => "dibix.configuration.schema";

            protected override void Read(string filePath, JObject json)
            {
                CollectSqlCodeAnalysisConfiguration(json, this._configuration.SqlCodeAnalysis);
                CollectEndpointConfiguration(json, this._configuration.Endpoints);
            }

            private static void CollectSqlCodeAnalysisConfiguration(JObject json, SqlCodeAnalysisConfiguration configuration)
            {
                const string sqlCodeAnalysisConfigurationName = "SqlCodeAnalysis";
                JObject sqlCodeAnalysisConfiguration = (JObject)json.Property(sqlCodeAnalysisConfigurationName)?.Value;
                if (sqlCodeAnalysisConfiguration == null) 
                    return;

                JProperty namingConventionPrefixProperty = sqlCodeAnalysisConfiguration.Property(nameof(SqlCodeAnalysisConfiguration.NamingConventionPrefix));
                if (namingConventionPrefixProperty != null)
                {
                    configuration.NamingConventionPrefix = (string)namingConventionPrefixProperty.Value;
                }
            }

            private static void CollectEndpointConfiguration(JObject json, EndpointConfiguration configuration)
            {
                const string endpointConfigurationName = "Endpoints";
                JObject endpointConfiguration = (JObject)json.Property(endpointConfigurationName)?.Value;
                if (endpointConfiguration == null)
                    return;

                JProperty baseUrlProperty = endpointConfiguration.Property(nameof(EndpointConfiguration.BaseUrl));
                if (baseUrlProperty != null)
                {
                    configuration.BaseUrl = (string)baseUrlProperty.Value;
                }
                
                JObject parameterSources = (JObject)endpointConfiguration.Property(nameof(EndpointConfiguration.ParameterSources))?.Value;
                if (parameterSources != null)
                {
                    foreach (JProperty parameterSource in parameterSources.Properties())
                    {
                        EndpointParameterSource httpParameterSource = new EndpointParameterSource(parameterSource.Name);

                        CollectParameterSource(parameterSource.Value.Type, parameterSource.Value, httpParameterSource);
                        configuration.ParameterSources.Add(httpParameterSource);
                    }
                }
            }

            private static void CollectParameterSource(JTokenType type, JToken value, EndpointParameterSource target)
            {
                switch (type)
                {
                    case JTokenType.Null:
                        target.IsDynamic = true;
                        break;

                    case JTokenType.Array:
                        JArray parameterSourceProperties = (JArray)value;
                        foreach (string parameterSourceProperty in parameterSourceProperties)
                        {
                            target.Properties.Add(parameterSourceProperty);
                        }
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            }
        }
    }
}