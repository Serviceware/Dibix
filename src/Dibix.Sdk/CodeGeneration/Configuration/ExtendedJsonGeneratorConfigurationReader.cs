using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ExtendedJsonGeneratorConfigurationReader : JsonGeneratorConfigurationReader
    {
        #region Fields
        private readonly IDictionary<string, Type> _parsers;
        private readonly IDictionary<string, Type> _formatters;
        #endregion

        #region Properties
        protected override string SchemaName => "dibix.schema.extended";
        #endregion

        #region Constructor
        public ExtendedJsonGeneratorConfigurationReader(string json, IFileSystemProvider fileSystemProvider, IErrorReporter errorReporter) : base(json, fileSystemProvider, errorReporter)
        {
            this._parsers = BuildTypeMap<ISqlStatementParser>();
            this._formatters = BuildTypeMap<ISqlStatementFormatter>();
        }
        #endregion

        #region Overrides
        protected override void Read(GeneratorConfiguration configuration, JObject root)
        {
            this.ReadInput(configuration, root);
            base.Read(configuration, root);
        }

        protected override void ReadDacPacProject(DacPacSourceConfiguration configuration, JObject json)
        {
            JObject include = (JObject)json.Property("include").Value;
            include.Properties().Each(x => configuration.AddStoredProcedure(x.Name, x.Value.Value<string>()));
        }

        protected override void ReadInputSource(InputSourceConfiguration source, JObject json)
        {
            this.ReadTextTransformation(source, json);
        }
        #endregion

        #region Private Methods
        private void ReadInput(GeneratorConfiguration configuration, JObject root)
        {
            base.ReadInputSources(configuration, root["input"]);
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
    }
}