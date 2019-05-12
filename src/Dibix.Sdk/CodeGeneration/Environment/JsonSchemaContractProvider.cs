using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class JsonSchemaContractProvider : IJsonSchemaProvider
    {
        private readonly ICollection<JsonContract> _schemas;
        private readonly IDictionary<string, JSchema> _definitions;

        public IEnumerable<JsonContract> Schemas => this._schemas;

        public JsonSchemaContractProvider(IFileSystemProvider fileSystemProvider)
        {
            this._schemas = new Collection<JsonContract>();
            this._definitions = new Dictionary<string, JSchema>();
            this.CollectSchemas(fileSystemProvider);
        }

        public bool TryGetSchemaDefinition(string schemaName, string definitionName, out JSchema schema)
        {
            return this._definitions.TryGetValue($"{schemaName}#{definitionName}", out schema);
        }

        private void CollectSchemas(IFileSystemProvider fileSystemProvider)
        {
            //// We assume that we are running in the context of a a sql project so we look for a neighbour contracts folder
            DirectoryInfo contractsDirectory = new DirectoryInfo(Path.Combine(fileSystemProvider.CurrentDirectory, "Contracts"));
            if (!contractsDirectory.Exists)
                return;

            foreach (FileInfo jsonSchemaFile in contractsDirectory.EnumerateFiles("*.json"))
            {
                using (Stream stream = File.OpenRead(jsonSchemaFile.FullName))
                {
                    using (TextReader textReader = new StreamReader(stream))
                    {
                        using (JsonReader schemaJsonReader = new JsonTextReader(textReader))
                        {
                            JSchema schema = JSchema.Load(schemaJsonReader);
                            if (!schema.ExtensionData.TryGetValue("definitions", out JToken definitionsExtension))
                                return;

                            JObject definitions = definitionsExtension as JObject;
                            if (definitions == null)
                                return;

                            foreach (JProperty definitionProperty in definitions.Properties())
                            {
                                using (JsonReader definitionJsonReader = new JTokenReader(definitionProperty.Value))
                                {
                                    JSchema definitionSchema = JSchema.Load(definitionJsonReader);
                                    string schemaName = Path.GetFileNameWithoutExtension(jsonSchemaFile.Name);
                                    string definitionName = definitionProperty.Name;
                                    this._schemas.Add(new JsonContract(schemaName, definitionName, definitionSchema));
                                    this._definitions.Add($"{schemaName}.{definitionName}", definitionSchema);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
