using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class JsonSqlAccessorGeneratorConfigurationReader : ISqlAccessorGeneratorConfigurationReader
    {
        #region Fields
        private const string SchemaName = "dibix.schema";
        private static readonly Lazy<JSchema> SchemaAccessor = new Lazy<JSchema>(LoadSchema);
        private readonly IExecutionEnvironment _environment;
        private readonly string _json;
        #endregion

        #region Constructor
        public JsonSqlAccessorGeneratorConfigurationReader(IExecutionEnvironment environment, string json)
        {
            this._environment = environment;
            this._json = json;
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

            foreach (JProperty property in ((JObject)json["input"]).Properties())
            {

            }
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
        #endregion
    }
}