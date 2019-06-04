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
        private readonly IDictionary<string, ContractDefinition> _definitions;
        #endregion

        #region Properties
        public ICollection<ContractDefinition> Contracts { get; }
        public bool HasSchemaErrors { get; private set; }
        #endregion

        #region Constructor
        public ContractDefinitionProvider(IFileSystemProvider fileSystemProvider, IErrorReporter errorReporter, IEnumerable<string> contracts)
        {
            this._errorReporter = errorReporter;
            this._definitions = new Dictionary<string, ContractDefinition>();
            this.Contracts = new Collection<ContractDefinition>();
            this.CollectSchemas(fileSystemProvider, contracts);
        }
        #endregion

        #region IContractDefinitionProvider Members
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
                            JObject contractJson = JObject.Load(jsonReader/*, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error }*/);

                            if (!contractJson.IsValid(JsonSchemaDefinition.GetSchema($"{this.GetType().Namespace}.Environment", SchemaName), out IList<ValidationError> errors))
                            {
                                foreach (ValidationError error in errors.Flatten())
                                {
                                    string errorMessage = $"[JSON] {error.Message} ({error.Path})";
                                    this._errorReporter.RegisterError(contractsFile.FullName, error.LineNumber, error.LinePosition, error.ErrorType.ToString(), errorMessage);
                                }
                                this.HasSchemaErrors = true;
                                continue;
                            }

                            this.ReadContracts(Path.GetFileNameWithoutExtension(contractsFile.Name), contractJson);
                        }
                    }
                }
            }
        }

        private void ReadContracts(string @namespace, JObject contracts)
        {
            foreach (JProperty definitionProperty in contracts.Properties())
            {
                this.ReadContract(@namespace, definitionProperty.Name, definitionProperty.Value);
            }
        }

        private void ReadContract(string @namespace, string definitionName, JToken value)
        {
            ContractDefinition definition = new ContractDefinition(@namespace, definitionName);
            switch (value.Type)
            {
                case JTokenType.Object:
                    foreach (JProperty property in ((JObject)value).Properties())
                        definition.Properties.Add(new ContractDefinitionProperty(property.Name, property.Value.Value<string>()));

                    this.Contracts.Add(definition);
                    this._definitions.Add($"{@namespace}#{definitionName}", definition);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value.Type, null);
            }
        }
        #endregion
    }
}
