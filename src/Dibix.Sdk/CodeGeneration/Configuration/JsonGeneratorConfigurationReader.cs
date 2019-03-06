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
    internal sealed class JsonGeneratorConfigurationReader : IGeneratorConfigurationReader
    {
        #region Fields
        private readonly string _json;
        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly IErrorReporter _errorReporter;
        private readonly IDictionary<string, Action<GeneratorConfiguration, JProperty>> _inputReaders;
        private readonly IDictionary<string, Type> _parsers;
        private readonly IDictionary<string, Type> _formatters;
        private readonly IDictionary<string, Type> _writers;
        #endregion

        #region Constructor
        public JsonGeneratorConfigurationReader(string json, IFileSystemProvider fileSystemProvider, IErrorReporter errorReporter)
        {
            this._json = json;
            this._fileSystemProvider = fileSystemProvider;
            this._errorReporter = errorReporter;
            this._inputReaders = new Dictionary<string, Action<GeneratorConfiguration, JProperty>>
            {
                { "^[\\w./]+[^.dacpac]$", this.ReadSqlProject }
              , { "^[\\w./]+.dacpac$",    this.ReadDacPac }
            };
            Assembly assembly = this.GetType().Assembly;
            this._parsers = BuildTypeMap<ISqlStatementParser>(assembly);
            this._formatters = BuildTypeMap<ISqlStatementFormatter>(assembly);
            this._writers = BuildTypeMap<IWriter>(assembly);
        }

        static JsonGeneratorConfigurationReader()
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

        #region IGeneratorConfigurationReader Members
        public void Read(GeneratorConfiguration configuration)
        {
            JObject json = JObject.Parse(this._json, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (!json.IsValid(JsonSchemaDefinition.Schema, out IList<ValidationError> errors))
            {
                foreach (ValidationError error in errors)
                {
                    string errorMessage = $"JSON configuration error: {error.Message}";
                    this._errorReporter.RegisterError(error.Path, error.LineNumber, error.LinePosition, error.ErrorType.ToString(), errorMessage);
                }
                configuration.IsInvalid = true;
                this._errorReporter.ReportErrors();
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
                           .Where(x => !x.IsInterface && !x.IsAbstract && typeof(T).IsAssignableFrom(x))
                           .ToDictionary(x => x.Name);
        }

        private void ReadInput(GeneratorConfiguration configuration, JToken token)
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

        private void ReadOutput(GeneratorConfiguration configuration, JObject json)
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

                string writerName = values.Value<string>("name")
                     , @namespace = values.Value<string>("namespace")
                     , className  = values.Value<string>("className")
                     , formatting = values.Value<string>("formatting");

                if (writerName != null)
                    configuration.Output.Writer = this._writers[writerName];

                if (@namespace != null)
                    configuration.Output.Namespace = @namespace;

                if (className != null)
                    configuration.Output.ClassName = className;

                if (formatting != null)
                    configuration.Output.Formatting = (CommandTextFormatting)Enum.Parse(typeof(CommandTextFormatting), formatting);
            }
        }

        private void ReadSqlProject(GeneratorConfiguration configuration, JProperty property)
        {
            this.ReadProject(configuration.Input, property, (projectName) => new PhysicalSourceConfiguration(this._fileSystemProvider, projectName), (source, value) =>
            {
                value.GetPropertyValues("include").Each(source.Include);
                value.GetPropertyValues("exclude").Each(source.Exclude);
            });
        }

        private void ReadDacPac(GeneratorConfiguration configuration, JProperty property)
        {
            this.ReadProject(configuration.Input, property, packagePath => new DacPacSourceConfiguration(this._fileSystemProvider, packagePath), (source, value) =>
            {
                JObject include = (JObject)value.Property("include").Value;
                include.Properties().Each(x => source.AddStoredProcedure(x.Name, x.Value.Value<string>()));
            });
        }

        private void ReadProject<T>(InputConfiguration configuration, JProperty property, Func<string, T> factory, Action<T, JObject> specificConfiguration) where T : InputSourceConfiguration
        {
            foreach (JObject group in property.Value.GetObjects())
            {
                T source = factory(property.Name);
                specificConfiguration(source, group);
                this.ReadTextTransformation(source, group);
                configuration.Sources.Add(source);
            }
        }

        private void ReadTextTransformation(InputSourceConfiguration source, JObject json)
        {
            string parser    = json.Value<string>("parser")
                 , formatter = json.Value<string>("formatter");

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
                Type type = typeof(JsonGeneratorConfigurationReader);
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