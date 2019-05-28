using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ContractDefinitionProvider : IContractDefinitionProvider
    {
        private readonly IDictionary<string, ContractDefinition> _definitions;

        public ICollection<ContractDefinition> Contracts { get; }

        public ContractDefinitionProvider(IFileSystemProvider fileSystemProvider, IEnumerable<string> contracts)
        {
            this._definitions = new Dictionary<string, ContractDefinition>();
            this.Contracts = new Collection<ContractDefinition>();
            this.CollectSchemas(fileSystemProvider, contracts);
        }

        public bool TryGetContract(string @namespace, string definitionName, out ContractDefinition schema)
        {
            return this._definitions.TryGetValue($"{@namespace}#{definitionName}", out schema);
        }

        private void CollectSchemas(IFileSystemProvider fileSystemProvider, IEnumerable<string> contracts)
        {
            // We assume that we are running in the context of a a sql project so we look for a neighbour contracts folder
            DirectoryInfo contractsDirectory = new DirectoryInfo(Path.Combine(fileSystemProvider.CurrentDirectory, "Contracts"));
            if (!contractsDirectory.Exists)
                return;

            foreach (FileInfo contractsFile in contracts.Select(x => new FileInfo(fileSystemProvider.GetPhysicalFilePath(null, x))))
            {
                using (Stream stream = File.OpenRead(contractsFile.FullName))
                {
                    using (TextReader textReader = new StreamReader(stream))
                    {
                        using (JsonReader jsonReader = new JsonTextReader(textReader))
                        {
                            JObject contractJson = JObject.Load(jsonReader);
                            foreach (JProperty definitionProperty in contractJson.Properties())
                            {
                                string schemaName = Path.GetFileNameWithoutExtension(contractsFile.Name);
                                string definitionName = definitionProperty.Name;

                                ContractDefinition definition = new ContractDefinition(schemaName, definitionName);
                                foreach (JProperty property in ((JObject)definitionProperty.Value).Properties())
                                    definition.Properties.Add(new ContractDefinitionProperty(property.Name, property.Value.Value<string>()));

                                this.Contracts.Add(definition);
                                this._definitions.Add($"{schemaName}#{definitionName}", definition);
                            }
                        }
                    }
                }
            }
        }
    }
}
