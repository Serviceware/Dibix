using System;
using System.Collections.Concurrent;
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
    public abstract class JsonGeneratorConfigurationReader : IGeneratorConfigurationReader
    {
        #region Fields
        private static readonly Assembly Assembly = typeof(JsonGeneratorConfigurationReader).Assembly;
        private readonly string _json;
        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly IErrorReporter _errorReporter;
        private readonly IDictionary<string, Func<GeneratorConfiguration, JProperty, IEnumerable<InputSourceConfiguration>>> _inputReaders;
        private readonly IDictionary<string, Type> _writers;
        #endregion

        #region Properties
        protected abstract string SchemaName { get; }
        #endregion

        #region Constructor
        protected JsonGeneratorConfigurationReader(string json, IFileSystemProvider fileSystemProvider, IErrorReporter errorReporter)
        {
            this._json = json;
            this._fileSystemProvider = fileSystemProvider;
            this._errorReporter = errorReporter;
            this._inputReaders = new Dictionary<string, Func<GeneratorConfiguration, JProperty, IEnumerable<InputSourceConfiguration>>>
            {
                { "^[\\w./]+[^.dacpac]$", this.ReadSqlProject }
              , { "^[\\w./]+.dacpac$",    this.ReadDacPac }
            };
            this._writers = BuildTypeMap<IWriter>();
        }
        #endregion

        #region IGeneratorConfigurationReader Members
        public void Read(GeneratorConfiguration configuration)
        {
            JObject json = JObject.Parse(this._json/*, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error }*/);
            if (!json.IsValid(JsonSchemaDefinition.GetSchema(this.SchemaName), out IList<ValidationError> errors))
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

            this.Read(configuration, json);
        }
        #endregion

        #region Protected Methods
        protected virtual void Read(GeneratorConfiguration configuration, JObject root)
        {
            this.ReadOutput(configuration, root);
        }

        protected void ReadInputSources(GeneratorConfiguration configuration, JToken json) => this.ReadInputSources(configuration, json, null);
        protected void ReadInputSources(GeneratorConfiguration configuration, JToken json, Action<InputSourceConfiguration> inputSourceConfigurator)
        {
            JObject input = (JObject)json;
            foreach (JProperty property in input.Properties())
            {
                var reader = this._inputReaders
                                 .Where(x => Regex.IsMatch(property.Name, x.Key))
                                 .Select(x => x.Value)
                                 .Single();

                foreach (InputSourceConfiguration inputSourceConfiguration in reader(configuration, property))
                {
                    inputSourceConfigurator?.Invoke(inputSourceConfiguration);
                }
            }
        }

        protected abstract void ReadDacPacProject(DacPacSourceConfiguration configuration, JObject json);

        protected virtual void ReadInputSource(InputSourceConfiguration source, JObject json) { }
        #endregion

        #region Private Methods
        private IEnumerable<InputSourceConfiguration> ReadSqlProject(GeneratorConfiguration configuration, JProperty property)
        {
            return this.ReadProject(configuration.Input, property, projectName => new PhysicalSourceConfiguration(this._fileSystemProvider, projectName), (source, value) =>
            {
                value.GetPropertyValues("include").Each(source.Include);
                value.GetPropertyValues("exclude").Each(source.Exclude);
            });
        }

        private IEnumerable<InputSourceConfiguration> ReadDacPac(GeneratorConfiguration configuration, JProperty property)
        {
            return this.ReadProject(configuration.Input, property, packagePath => new DacPacSourceConfiguration(this._fileSystemProvider, packagePath), this.ReadDacPacProject);
        }

        private IEnumerable<T> ReadProject<T>(InputConfiguration configuration, JProperty property, Func<string, T> factory, Action<T, JObject> specificConfiguration) where T : InputSourceConfiguration
        {
            foreach (JObject group in property.Value.GetObjects())
            {
                T source = factory(property.Name);
                specificConfiguration(source, group);
                this.ReadInputSource(source, group);
                configuration.Sources.Add(source);
                yield return source;
            }
        }

        private void ReadOutput(GeneratorConfiguration configuration, JObject root)
        {
            JToken output = root["output"];
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

        protected static IDictionary<string, Type> BuildTypeMap<T>()
        {
            return Assembly.GetTypes()
                           .Where(x => !x.IsInterface && !x.IsAbstract && typeof(T).IsAssignableFrom(x))
                           .ToDictionary(x => x.Name);
        }
        #endregion

        #region Nested Types
        private static class JsonSchemaDefinition
        {
            private static readonly ConcurrentDictionary<string, JSchema> Schemas = new ConcurrentDictionary<string, JSchema>();

            public static JSchema GetSchema(string schema) => Schemas.GetOrAdd(schema, LoadSchema);

            private static JSchema LoadSchema(string schemaName)
            {
                Type type = typeof(JsonGeneratorConfigurationReader);
                string resourcePath = $"{type.Namespace}.Configuration.{schemaName}.json";
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