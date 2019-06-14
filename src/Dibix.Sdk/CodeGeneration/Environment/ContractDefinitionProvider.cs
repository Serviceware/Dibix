using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ContractDefinitionProvider : IContractDefinitionProvider
    {
        #region Fields
        private const string SchemaName = "dibix.contracts.schema";
        private readonly IErrorReporter _errorReporter;
        private readonly bool _multipleAreas;
        private readonly string _layerName;
        private readonly IDictionary<string, ContractDefinition> _definitions;
        #endregion

        #region Properties
        public ICollection<ContractDefinition> Contracts { get; }
        public bool HasSchemaErrors { get; private set; }
        #endregion

        #region Constructor
        public ContractDefinitionProvider(IFileSystemProvider fileSystemProvider, IErrorReporter errorReporter, IEnumerable<string> contracts, bool multipleAreas, string layerName)
        {
            this._errorReporter = errorReporter;
            this._multipleAreas = multipleAreas;
            this._layerName = layerName;
            this._definitions = new Dictionary<string, ContractDefinition>();
            this.Contracts = new Collection<ContractDefinition>();
            this.CollectSchemas(fileSystemProvider, contracts);
        }
        #endregion

        #region IContractDefinitionProvider Members
        public bool TryGetContract(string contractName, out ContractDefinition schema)
        {
            return this._definitions.TryGetValue(contractName, out schema);
        }

        private void CollectSchemas(IFileSystemProvider fileSystemProvider, IEnumerable<string> contracts)
        {
            foreach (string contractsFile in fileSystemProvider.GetFiles(null, contracts.Select(x => (VirtualPath)x), new VirtualPath[0]))
            {
                using (Stream stream = File.OpenRead(contractsFile))
                {
                    using (TextReader textReader = new StreamReader(stream))
                    {
                        using (JsonReader jsonReader = new JsonTextReader(textReader))
                        {
                            JObject contractJson = JObject.Load(jsonReader/*, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error }*/);

                            if (!contractJson.IsValid(JsonSchemaDefinition.GetSchema($"{this.GetType().Namespace}.Environment", SchemaName), out IList<ValidationError> errors))
                            {
                                foreach (ValidationError error in errors.Flatten())
                                {
                                    string errorMessage = $"[JSON] {error.Message} ({error.Path})";
                                    this._errorReporter.RegisterError(contractsFile, error.LineNumber, error.LinePosition, error.ErrorType.ToString(), errorMessage);
                                }
                                this.HasSchemaErrors = true;
                                continue;
                            }

                            string virtualPath = Path.GetDirectoryName(contractsFile).Substring(fileSystemProvider.CurrentDirectory.Length + 1);
                            int virtualPathRootIndex = virtualPath.IndexOf(Path.DirectorySeparatorChar) + 1;
                            string namespaceKey = String.Empty;
                            if (virtualPathRootIndex > 0)
                            {
                                string virtualPathRoot = virtualPath.Substring(virtualPathRootIndex);
                                namespaceKey = virtualPathRoot.Replace(Path.DirectorySeparatorChar, '.');
                            }
                            string @namespace = NamespaceUtility.BuildNamespace(namespaceKey, this._multipleAreas, this._layerName);
                            this.ReadContracts(namespaceKey, @namespace, contractJson);
                        }
                    }
                }
            }
        }

        private void ReadContracts(string namespaceKey, string @namespace, JObject contracts)
        {
            foreach (JProperty definitionProperty in contracts.Properties())
            {
                string key = $"{(!String.IsNullOrEmpty(namespaceKey) ? $"{namespaceKey}." : null)}{definitionProperty.Name}";
                this.ReadContract(key, @namespace, definitionProperty.Name, definitionProperty.Value);
            }
        }

        private void ReadContract(string key, string @namespace, string definitionName, JToken value)
        {
            switch (value.Type)
            {
                case JTokenType.Object:
                    this.ReadObjectContract(key, @namespace, definitionName, value);
                    break;

                case JTokenType.Array:
                    this.ReadEnumContract(key, @namespace, definitionName, value);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(value.Type), value.Type, null);
            }
        }

        private void ReadObjectContract(string key, string @namespace, string definitionName, JToken value)
        {
            ObjectContract contract = new ObjectContract(@namespace, definitionName);
            foreach (JProperty property in ((JObject)value).Properties())
                contract.Properties.Add(new ObjectContractProperty(property.Name, property.Value.Value<string>()));

            this.Contracts.Add(contract);
            this._definitions.Add(key, contract);
        }

        private void ReadEnumContract(string key, string @namespace, string definitionName, JToken value)
        {
            EnumContract contract = new EnumContract(@namespace, definitionName, false);
            foreach (JToken child in (JArray)value)
            {
                switch (child.Type)
                {
                    case JTokenType.Object:
                        JProperty property = ((JObject)child).Properties().Single();
                        contract.Members.Add(new EnumContractMember(property.Name, property.Value.Value<string>()));
                        break;

                    case JTokenType.String:
                        contract.Members.Add(new EnumContractMember(child.Value<string>()));
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(child.Type), child.Type, null);
                }
            }

            this.Contracts.Add(contract);
            this._definitions.Add(key, contract);
        }
        #endregion
    }
}
