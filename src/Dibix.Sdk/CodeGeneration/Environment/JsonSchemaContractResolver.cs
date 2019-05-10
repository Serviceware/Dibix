using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class JsonSchemaContractResolver : IContractResolver
    {
        #region Fields
        private readonly IFileSystemProvider _fileSystemProvider;
        #endregion

        #region Constructor
        public JsonSchemaContractResolver(IFileSystemProvider fileSystemProvider)
        {
            this._fileSystemProvider = fileSystemProvider;
        }
        #endregion

        #region IContractResolver Members
        public ContractInfo ResolveContract(string input, Action<string> errorHandler)
        {
            if (input[0] != '#')
                return null;

            string normalizedInput = input.Substring(1, input.Length - 1);
            string[] parts = normalizedInput.Split('.');
            if (parts.Length != 2)
                return null;

            string schemaName = parts[0];
            string definitionName = parts[1];
            string schemaPath = this._fileSystemProvider.GetPhysicalFilePath(null, $"Contracts/{schemaName}.json");
            using (Stream stream = File.OpenRead(schemaPath))
            {
                using (TextReader textReader = new StreamReader(stream))
                {
                    using (JsonReader schemaJsonReader = new JsonTextReader(textReader))
                    {
                        JSchema schema = JSchema.Load(schemaJsonReader);
                        if (!schema.ExtensionData.TryGetValue("definitions", out JToken definitionsExtension))
                            return null;

                        JObject definitions = definitionsExtension as JObject;
                        JProperty definitionProperty = definitions?.Property(definitionName);
                        if (definitionProperty == null)
                            return null;

                        using (JsonReader definitionJsonReader = new JTokenReader(definitionProperty.Value))
                        {
                            JSchema definitionSchema = JSchema.Load(definitionJsonReader);
                            ContractName contractName = new ContractName(input, definitionName);
                            ContractInfo contract = new ContractInfo(contractName, false);
                            contract.Schema = definitionSchema;
                            foreach (KeyValuePair<string, JSchema> property in definitionSchema.Properties)
                                contract.Properties.Add(property.Key);

                            return contract;
                        }
                    }
                }
            }
        }
        #endregion
    }
}