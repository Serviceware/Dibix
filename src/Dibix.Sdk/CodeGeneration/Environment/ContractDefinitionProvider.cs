using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ContractDefinitionProvider : JsonSchemaDefinitionReader, IContractDefinitionProvider
    {
        #region Fields
        private readonly bool _multipleAreas;
        private readonly IDictionary<string, ContractDefinition> _definitions;
        #endregion

        #region Properties
        public ICollection<ContractDefinition> Contracts { get; }
        protected override string SchemaName => "dibix.contracts.schema";
        #endregion

        #region Constructor
        public ContractDefinitionProvider(IFileSystemProvider fileSystemProvider, IErrorReporter errorReporter, IEnumerable<string> contracts, bool multipleAreas) : base(fileSystemProvider, errorReporter)
        {
            this._multipleAreas = multipleAreas;
            this._definitions = new Dictionary<string, ContractDefinition>();
            this.Contracts = new Collection<ContractDefinition>();
            base.Collect(contracts);
        }
        #endregion

        #region IContractDefinitionProvider Members
        public bool TryGetContract(string contractName, out ContractDefinition schema)
        {
            return this._definitions.TryGetValue(contractName, out schema);
        }
        #endregion

        #region Overrides
        protected override void Read(string filePath, JObject json)
        {
            string virtualPath = Path.GetDirectoryName(filePath).Substring(base.FileSystemProvider.CurrentDirectory.Length + 1);
            int virtualPathRootIndex = virtualPath.IndexOf(Path.DirectorySeparatorChar) + 1;
            string namespaceKey = String.Empty;
            if (virtualPathRootIndex > 0)
            {
                string virtualPathRoot = virtualPath.Substring(virtualPathRootIndex);
                namespaceKey = virtualPathRoot.Replace(Path.DirectorySeparatorChar, '.');
            }
            string @namespace = NamespaceUtility.BuildNamespace(namespaceKey, this._multipleAreas, LayerName.DomainModel);
            this.ReadContracts(namespaceKey, @namespace, json);
        }
        #endregion

        #region Private Methods
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
            {
                if (property.Name == "$wcfNs")
                {
                    string wcfNamespace = (string)property.Value;
                    if (!wcfNamespace.StartsWith("http://", StringComparison.Ordinal))
                        wcfNamespace = $"http://schemas.datacontract.org/2004/07/{wcfNamespace}";

                    contract.WcfNamespace = wcfNamespace;
                }
                else
                {
                    string typeName;
                    bool isPartOfKey = false;
                    switch (property.Value.Type)
                    {
                        case JTokenType.Object:
                            JObject propertyInfo = (JObject)property.Value;
                            typeName = (string)propertyInfo.Property("type").Value;
                            isPartOfKey = (bool?)propertyInfo.Property("isPartOfKey")?.Value ?? default;
                            break;

                        case JTokenType.String:
                            typeName = (string)property.Value;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(property.Type), property.Type, null);
                    }
                    bool isEnumerable = TryGetArrayType(typeName, ref typeName);
                    contract.Properties.Add(new ObjectContractProperty(property.Name, typeName, isPartOfKey, isEnumerable));
                }
            }

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

        private static bool TryGetArrayType(string type, ref string arrayType)
        {
            int index = type.LastIndexOf('*');
            if (index < 0)
                return false;

            arrayType = type.Substring(0, index);
            return true;
        }
        #endregion
    }
}
