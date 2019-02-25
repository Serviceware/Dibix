using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class JsonSqlAccessorGeneratorConfigurationReader : ISqlAccessorGeneratorConfigurationReader
    {
        #region Fields
        private static readonly IDictionary<string, Type> Parsers = new[]
        {
            typeof(NoOpParser)
          , typeof(SqlStoredProcedureParser)
        }.ToDictionary(x => x.Name);
        private static readonly IDictionary<string, Type> Formatters = new[]
        {
            typeof(TakeSourceSqlStatementFormatter)
          , typeof(GenerateScriptSqlStatementFormatter)
          , typeof(ExecStoredProcedureSqlStatementFormatter)
        }.ToDictionary(x => x.Name);
        private const string SchemaName = "dibix.schema";
        private static readonly Lazy<JSchema> SchemaAccessor = new Lazy<JSchema>(LoadSchema);
        private readonly IExecutionEnvironment _environment;
        private readonly string _json;
        private readonly IDictionary<string, Action<SqlAccessorGeneratorConfiguration, JProperty>> _inputReaders;
        #endregion

        #region Constructor
        public JsonSqlAccessorGeneratorConfigurationReader(IExecutionEnvironment environment, string json)
        {
            this._environment = environment;
            this._json = json;
            this._inputReaders = new Dictionary<string, Action<SqlAccessorGeneratorConfiguration, JProperty>>
            {
                { "^[\\w./]+[^.dacpac]$", this.ReadSqlProject }
              , { "^[\\w./]+.dacpac$",    this.ReadDacPac }
            };
        }
        #endregion

        #region ISqlAccessorGeneratorConfigurationReader Members
        public void Read(SqlAccessorGeneratorConfiguration configuration)
        {
            JToken json = JToken.Parse(this._json);
            if (!json.IsValid(SchemaAccessor.Value, out IList<ValidationError> errors))
            {
                errors.Each(x =>
                {
                    string errorMessage = $"JSON configuration error: {x.Message}";
                    this._environment.RegisterError(x.Path, x.LineNumber, x.LinePosition, x.ErrorType.ToString(), errorMessage);
                });
                return;
            }

            this.ReadInput(configuration, json);
        }
        #endregion

        #region Private Methods
        private static JSchema LoadSchema()
        {
            Type type = typeof(JsonSqlAccessorGeneratorConfigurationReader);
            string resourcePath = $"{type.Namespace}.Configuration.{SchemaName}.json";
            using (Stream stream = type.Assembly.GetManifestResourceStream(resourcePath))
            {
                using (TextReader textReader = new StreamReader(stream))
                {
                    using (JsonReader jsonReader = new JsonTextReader(textReader))
                    {
                        return JSchema.Load(jsonReader);
                    }
                }
            }
        }

        private void ReadInput(SqlAccessorGeneratorConfiguration configuration, JToken token)
        {
            foreach (JProperty property in ((JObject)token["input"]).Properties())
            {
                var reader = this._inputReaders
                                 .Where(x => Regex.IsMatch(property.Name, x.Key))
                                 .Select(x => x.Value)
                                 .Single();
                reader(configuration, property);
            }
        }

        private void ReadSqlProject(SqlAccessorGeneratorConfiguration configuration, JProperty property)
        {
            PhysicalSourceSelection source = new PhysicalSourceSelection(this._environment, property.Name);
            if (property.Value.Type != JTokenType.Null)
            {
                JObject value = (JObject)property.Value;
                (value.Property("include")?.GetValues() ?? Enumerable.Empty<string>()).Each(source.Include);
                (value.Property("exclude")?.GetValues() ?? Enumerable.Empty<string>()).Each(source.Exclude);

                ReadTextTransformation(source, value);
            }

            configuration.Sources.Add(source);
        }

        private void ReadDacPac(SqlAccessorGeneratorConfiguration configuration, JProperty property)
        {
            DacPacSelection source = new DacPacSelection(this._environment, property.Name);
            JObject value = (JObject)property.Value;
            JObject include = (JObject)value.Property("include").Value;
            include.Properties().Each(x => source.AddStoredProcedure(x.Name, x.Value.Value<string>()));

            ReadTextTransformation(source, value);
            configuration.Sources.Add(source);
        }

        private static void ReadTextTransformation(SourceSelection source, JObject json)
        {
            source.Parser = CreateInstanceByKey<ISqlStatementParser>("parser", json, Parsers);
            source.Formatter = CreateInstanceByKey<ISqlStatementFormatter>("formatter", json, Formatters);
        }

        private static T CreateInstanceByKey<T>(string propertyName, JObject json, IDictionary<string, Type> typeLookup) where T : class
        {
            string key = json.Property(propertyName)?.Value.Value<string>();
            if (key == null)
                return null;

            Type type = typeLookup[key];
            return (T)Activator.CreateInstance(type);
        }
        #endregion
    }
}