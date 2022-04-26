using System;
using System.IO;
using System.Linq;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Sdk.CodeGeneration;
using Dibix.Sdk.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk
{
    internal sealed class SqlCoreTaskConfiguration
    {
        public SqlCodeAnalysisConfiguration SqlCodeAnalysis { get; } = new SqlCodeAnalysisConfiguration();
        public EndpointConfiguration Endpoints { get; } = new EndpointConfiguration();

        private SqlCoreTaskConfiguration() { }

        public static SqlCoreTaskConfiguration Create(string filePath, IActionParameterSourceRegistry actionParameterSourceRegistry, IActionParameterConverterRegistry actionParameterConverterRegistry, IFileSystemProvider fileSystemProvider, ILogger logger)
        {
            SqlCoreTaskConfiguration configuration = new SqlCoreTaskConfiguration();
            if (!File.Exists(filePath))
            {
                return configuration;
            }

            SqlCoreTaskConfigurationReader configurationReader = new SqlCoreTaskConfigurationReader(filePath, configuration, actionParameterSourceRegistry, actionParameterConverterRegistry, fileSystemProvider, logger);
            configurationReader.Collect();
            return configuration;
        }

        private sealed class SqlCoreTaskConfigurationReader : JsonSchemaDefinitionReader
        {
            private readonly string _filePath;
            private readonly SqlCoreTaskConfiguration _configuration;
            private readonly IActionParameterSourceRegistry _actionParameterSourceRegistry;
            private readonly IActionParameterConverterRegistry _actionParameterConverterRegistry;
            private readonly ILogger _logger;

            public SqlCoreTaskConfigurationReader(string filePath, SqlCoreTaskConfiguration configuration, IActionParameterSourceRegistry actionParameterSourceRegistry, IActionParameterConverterRegistry actionParameterConverterRegistry, IFileSystemProvider fileSystemProvider, ILogger logger) : base(fileSystemProvider, logger)
            {
                this._filePath = filePath;
                this._configuration = configuration;
                this._actionParameterSourceRegistry = actionParameterSourceRegistry;
                this._actionParameterConverterRegistry = actionParameterConverterRegistry;
                this._logger = logger;
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

            private void CollectEndpointConfiguration(JObject json, EndpointConfiguration configuration)
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

                const string parameterSourcesName = "ParameterSources";
                JObject parameterSources = (JObject)endpointConfiguration.Property(parameterSourcesName)?.Value;
                if (parameterSources != null)
                {
                    foreach (JProperty parameterSource in parameterSources.Properties())
                    {
                        if (this._actionParameterSourceRegistry.TryGetDefinition(parameterSource.Name, out ActionParameterSourceDefinition _))
                        {
                            IJsonLineInfo lineInfo = parameterSource.GetLineInfo();
                            this._logger.LogError($"Parameter source '{parameterSource.Name}' is already registered", this._filePath, lineInfo.LineNumber, lineInfo.LinePosition);
                            continue;
                        }

                        CollectParameterSource(parameterSource.Name, parameterSource.Value.Type, parameterSource.Value, this._actionParameterSourceRegistry);
                    }
                }

                const string convertersName = "Converters";
                JArray converters = (JArray)endpointConfiguration.Property(convertersName)?.Value;
                if (converters != null)
                {
                    foreach (JToken converter in converters)
                    {
                        string converterName = (string)converter;
                        if (this._actionParameterConverterRegistry.IsRegistered(converterName))
                        {
                            IJsonLineInfo lineInfo = converter.GetLineInfo();
                            this._logger.LogError($"Parameter converter '{converterName}' is already registered", this._filePath, lineInfo.LineNumber, lineInfo.LinePosition);
                            continue;
                        }

                        this._actionParameterConverterRegistry.Register(converterName);
                    }
                }
            }

            private static void CollectParameterSource(string sourceName, JTokenType valueType, JToken value, IActionParameterSourceRegistry registry)
            {
                switch (valueType)
                {
                    case JTokenType.Null:
                        registry.Register(new DynamicParameterSource(sourceName), x => new DynamicParameterSourceValidator(x));
                        break;

                    case JTokenType.Array:
                        JArray parameterSourceProperties = (JArray)value;
                        string[] propertyNames = parameterSourceProperties.Select(x => (string)x).ToArray();
                        registry.Register(new DynamicPropertyParameterSource(sourceName, propertyNames), x => new DynamicPropertyParameterSourceValidator(x));
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(valueType), valueType, null);
                }
            }
        }
    }
}