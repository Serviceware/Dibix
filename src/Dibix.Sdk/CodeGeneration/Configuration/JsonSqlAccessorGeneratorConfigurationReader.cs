using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class JsonSqlAccessorGeneratorConfigurationReader : ISqlAccessorGeneratorConfigurationReader
    {
        #region Fields
        private readonly IExecutionEnvironment _environment;
        private readonly string _json;
        private readonly IDictionary<string, Action<SqlAccessorGeneratorConfiguration, JProperty>> _inputReaders;
        private readonly IDictionary<string, Type> _parsers;
        private readonly IDictionary<string, Type> _formatters;
        private readonly IDictionary<string, Type> _writers;
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
            Assembly assembly = this.GetType().Assembly;
            this._parsers = BuildTypeMap<ISqlStatementParser>(assembly);
            this._formatters = BuildTypeMap<ISqlStatementFormatter>(assembly);
            this._writers = BuildTypeMap<IWriter>(assembly);
        }

        static JsonSqlAccessorGeneratorConfigurationReader()
        {
            // Newtonsoft.Json.Schema uses an older Newtonsoft.Json version
            // We need Newtonsoft.Json 12 though, because of JsonLoadSettings.DuplicatePropertyNameHandling
            Assembly OnAssemblyResolve(object sender, ResolveEventArgs e)
            {
                AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
                AssemblyName requestedAssembly = new AssemblyName(e.Name);
                if (requestedAssembly.Name != "Newtonsoft.Json")
                    return null;

                requestedAssembly.Version = new Version(12, 0, 0, 0);
                return Assembly.Load(requestedAssembly);
            }

            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }
        #endregion

        #region ISqlAccessorGeneratorConfigurationReader Members
        public void Read(SqlAccessorGeneratorConfiguration configuration)
        {
            JObject json = JObject.Parse(this._json, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (!json.IsValid(JsonSchemaDefinition.Schema, out IList<ValidationError> errors))
            {
                errors.Each(x =>
                {
                    string errorMessage = $"JSON configuration error: {x.Message}";
                    this._environment.RegisterError(x.Path, x.LineNumber, x.LinePosition, x.ErrorType.ToString(), errorMessage);
                });
                return;
            }

            this.ReadInput(configuration, json);
            this.ReadOutput(configuration, json);
        }
        #endregion

        #region Private Methods
        private static IDictionary<string, Type> BuildTypeMap<T>(Assembly assembly)
        {
            return assembly.GetTypes()
                           .Where(typeof(T).IsAssignableFrom)
                           .ToDictionary(x => x.Name);
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

        private void ReadOutput(SqlAccessorGeneratorConfiguration configuration, JObject json)
        {
            JToken output = json["output"];
            if (output == null)
                return;

            if (output.Type == JTokenType.String)
            {
                string writerName = output.Value<string>();
                configuration.Output.Writer = this._writers[writerName];
            }
            else
            {
                JObject values = (JObject)output;
                string writerName = values.Value<string>("name");
                configuration.Output.Writer = this._writers[writerName];

                string @namespace = values.Value<string>("namespace")
                     , className  = values.Value<string>("className");
                if (@namespace != null)
                    configuration.Output.Namespace = @namespace;
                if (className != null)
                    configuration.Output.ClassName = className;

                string formattingValue = values.Value<string>("formatting");
                if (formattingValue != null)
                    configuration.Output.Formatting = (SqlQueryOutputFormatting)Enum.Parse(typeof(SqlQueryOutputFormatting), formattingValue);
            }
        }

        private void ReadSqlProject(SqlAccessorGeneratorConfiguration configuration, JProperty property)
        {
            PhysicalSourceConfiguration source = new PhysicalSourceConfiguration(this._environment, property.Name);
            if (property.Value.Type != JTokenType.Null)
            {
                JObject value = (JObject)property.Value;
                (value.Property("include")?.GetValues() ?? Enumerable.Empty<string>()).Each(source.Include);
                (value.Property("exclude")?.GetValues() ?? Enumerable.Empty<string>()).Each(source.Exclude);

                this.ReadTextTransformation(source, value);
            }

            configuration.Input.Sources.Add(source);
        }

        private void ReadDacPac(SqlAccessorGeneratorConfiguration configuration, JProperty property)
        {
            DacPacSourceConfiguration source = new DacPacSourceConfiguration(this._environment, property.Name);
            JObject value = (JObject)property.Value;
            JObject include = (JObject)value.Property("include").Value;
            include.Properties().Each(x => source.AddStoredProcedure(x.Name, x.Value.Value<string>()));

            this.ReadTextTransformation(source, value);
            configuration.Input.Sources.Add(source);
        }

        private void ReadTextTransformation(SourceConfiguration source, JObject json)
        {
            string parser    = json.Value<string>("parser")
                 , formatter = json.Value<string>("parser");

            if (parser != null)
                source.Parser = this._parsers[parser];

            if (formatter != null)
                source.Formatter = this._formatters[formatter];
        }
        #endregion

        #region Nested Types
        private static class JsonSchemaDefinition
        {
            private static readonly Lazy<JSchema> SchemaAccessor = new Lazy<JSchema>(LoadSchema);
            private const string SchemaName = "dibix.schema";

            public static JSchema Schema => SchemaAccessor.Value;

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
        }
        #endregion
    }
}