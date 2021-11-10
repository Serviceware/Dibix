using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Dibix.Sdk.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ContractDefinitionProvider : JsonSchemaDefinitionReader, IContractDefinitionProvider
    {
        #region Fields
        private const string RootFolderName = "Contracts";
        private readonly string _productName;
        private readonly string _areaName;
        private readonly IDictionary<string, ContractDefinition> _contracts;
        private readonly ICollection<IDelayedContractCollectionAction> _delayedCollectionActions;
        #endregion

        #region Properties
        public IEnumerable<ContractDefinition> Contracts => this._contracts.Values;
        protected override string SchemaName => "dibix.contracts.schema";
        IEnumerable<SchemaDefinition> ISchemaProvider.Schemas => this.Contracts.Select(x => x.Schema);
        #endregion

        #region Constructor
        public ContractDefinitionProvider(IFileSystemProvider fileSystemProvider, ILogger logger, IEnumerable<string> contracts, string productName, string areaName) : base(fileSystemProvider, logger)
        {
            this._productName = productName;
            this._areaName = areaName;
            this._contracts = new Dictionary<string, ContractDefinition>();
            this._delayedCollectionActions = new Collection<IDelayedContractCollectionAction>();
            this.CollectCore(contracts);
        }
        #endregion

        #region ISchemaProvider Members
        public bool TryGetSchema(string name, out SchemaDefinition schema)
        {
            if (!this._contracts.TryGetValue(name, out ContractDefinition contract))
            {
                // Assume relative namespace
                string absoluteNamespace = NamespaceUtility.BuildAbsoluteNamespace(this._productName, this._areaName, LayerName.DomainModel, name);
                if (!this._contracts.TryGetValue(absoluteNamespace, out contract))
                {
                    schema = null;
                    return false;
                }
            }

            contract.IsUsed = true;
            schema = contract.Schema;
            return true;
        }
        #endregion

        #region Overrides
        protected override void Read(string filePath, JObject json)
        {
            string relativePath = Path.GetDirectoryName(filePath).Substring(base.FileSystemProvider.CurrentDirectory.Length + 1);
            string[] parts = relativePath.Split(Path.DirectorySeparatorChar);
            if (parts[0] != RootFolderName)
                throw new InvalidOperationException($"Expected contract root folder to be '{RootFolderName}' but got: {parts[0]}");

            bool multipleAreas = this._areaName == null;
            if (multipleAreas && parts.Length < 2)
                throw new InvalidOperationException(@"Expected the following folder structure for projects with multiple areas: Contracts\Area\*.json
If this is not a project that has multiple areas, please make sure to define the <RootNamespace> tag in the following format: Product.Area");

            string relativeNamespace = String.Join(".", parts.Skip(1));
            string currentNamespace = NamespaceUtility.BuildAbsoluteNamespace(this._productName, this._areaName, LayerName.DomainModel, relativeNamespace);

            string root = multipleAreas ? parts[1] : null;
            string rootNamespace = NamespaceUtility.BuildAbsoluteNamespace(this._productName, this._areaName, LayerName.DomainModel, root);

            this.ReadContracts(rootNamespace, currentNamespace, json, filePath);
        }
        #endregion

        #region Private Methods
        private void CollectCore(IEnumerable<string> contracts)
        {
            base.Collect(contracts);

            foreach (IDelayedContractCollectionAction action in this._delayedCollectionActions) 
                action.Invoke();
        }

        private void ReadContracts(string rootNamespace, string currentNamespace, JObject contracts, string filePath)
        {
            foreach (JProperty definitionProperty in contracts.Properties())
            {
                this.ReadContract(rootNamespace, currentNamespace, definitionProperty.Name, definitionProperty.Value, filePath, definitionProperty.GetLineInfo());
            }
        }

        private void ReadContract(string rootNamespace, string currentNamespace, string definitionName, JToken value, string filePath, IJsonLineInfo lineInfo)
        {
            switch (value.Type)
            {
                case JTokenType.Object:
                    this.ReadObjectContract(rootNamespace, currentNamespace, definitionName, value, filePath, lineInfo);
                    break;

                case JTokenType.Array:
                    this.ReadEnumContract(currentNamespace, definitionName, value, filePath, lineInfo);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(value.Type), value.Type, null);
            }
        }

        private void ReadObjectContract(string rootNamespace, string currentNamespace, string definitionName, JToken value, string filePath, IJsonLineInfo lineInfo)
        {
            ObjectSchema contract = new ObjectSchema(currentNamespace, definitionName, SchemaDefinitionSource.Local);
            foreach (JProperty property in ((JObject)value).Properties())
            {
                if (property.Name == "$wcfNs")
                    contract.WcfNamespace = (string)property.Value;
                else
                {
                    JValue typeNameValue;
                    string typeName;
                    bool isPartOfKey = default;
                    bool isOptional = default;
                    bool isDiscriminator = default;
                    JValue defaultValue = default;
                    bool obfuscated = default;
                    SerializationBehavior serializationBehavior = default;
                    DateTimeKind dateTimeKind = default;
                    switch (property.Value.Type)
                    {
                        case JTokenType.Object:
                            JObject propertyInfo = (JObject)property.Value;
                            typeNameValue = (JValue)propertyInfo.Property("type").Value;
                            typeName = (string)typeNameValue;
                            isPartOfKey = (bool?)propertyInfo.Property("isPartOfKey")?.Value ?? default;
                            isOptional = (bool?)propertyInfo.Property("isOptional")?.Value ?? default;
                            isDiscriminator = (bool?)propertyInfo.Property("isDiscriminator")?.Value ?? default;
                            defaultValue = (JValue)propertyInfo.Property("default")?.Value;
                            obfuscated = (bool?)propertyInfo.Property("obfuscated")?.Value ?? default;

                            Enum.TryParse((string)propertyInfo.Property("serialize")?.Value, true, out serializationBehavior);
                            Enum.TryParse((string)propertyInfo.Property("kind")?.Value, true, out dateTimeKind);
                            break;

                        case JTokenType.String:
                            typeNameValue = (JValue)property.Value;
                            typeName = (string)typeNameValue;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(property.Type), property.Type, null);
                    }

                    TypeReference type = this.ParsePropertyType(typeName, rootNamespace, filePath, typeNameValue);
                    ObjectSchemaProperty objectSchemaProperty = new ObjectSchemaProperty(property.Name, type, isPartOfKey, isOptional, isDiscriminator, serializationBehavior: serializationBehavior, dateTimeKind: dateTimeKind, obfuscated: obfuscated);
                    if (defaultValue != null)
                    {
                        // Delay default value resolution, since property defaults might reference enum contracts, that have not been registered yet
                        this._delayedCollectionActions.Add(new ContractDefaultValueCollectionAction(objectSchemaProperty, defaultValue, filePath, this._contracts, this.Logger));
                    }
                    
                    contract.Properties.Add(objectSchemaProperty);
                }
            }

            this.CollectContract(contract, filePath, lineInfo);
        }

        private void ReadEnumContract(string currentNamespace, string definitionName, JToken definitionValue, string filePath, IJsonLineInfo lineInfo)
        {
            EnumSchema contract = new EnumSchema(currentNamespace, definitionName, SchemaDefinitionSource.Local, isFlaggable: false);

            ICollection<EnumValue> values = ReadEnumValues(definitionValue).ToArray();
            IDictionary<string, int> actualValues = values.Where(x => x.ActualValue.HasValue).ToDictionary(x => x.Name, x => x.ActualValue.Value);

            foreach (EnumValue value in values)
            {
                bool foundCombinationFlag = false;
                int actualValue = value.ActualValue ?? EnumValueParser.ParseDynamicValue(actualValues, value.StringValue, ref foundCombinationFlag);

                if (foundCombinationFlag)
                {
                    // Currently there is no explicit support to mark an enum as flaggable.
                    // Therefore this is the only case where we implicitly detect it.
                    contract.IsFlaggable = true;
                }

                contract.Members.Add(new EnumSchemaMember(value.Name, actualValue, value.StringValue, contract));
            }

            this.CollectContract(contract, filePath, lineInfo);
        }

        private void CollectContract(SchemaDefinition definition, string filePath, IJsonLineInfo lineInfo)
        {
            string name = definition.FullName;
            if (this._contracts.TryGetValue(name, out ContractDefinition otherContract))
            {
                this.Logger.LogError(null, $"Ambiguous contract definition: {definition.FullName}", otherContract.FilePath, otherContract.Line, otherContract.Column);
                this.Logger.LogError(null, $"Ambiguous contract definition: {definition.FullName}", filePath, lineInfo.LineNumber, lineInfo.LinePosition);
                return;
            }

            ContractDefinition contractDefinition = new ContractDefinition(definition, filePath, lineInfo.LineNumber, lineInfo.LinePosition);
            this._contracts.Add(name, contractDefinition);
        }

        private static IEnumerable<EnumValue> ReadEnumValues(JToken members)
        {
            for (int i = 0; i < ((JArray)members).Count; i++)
            {
                JToken member = ((JArray)members)[i];
                yield return ReadEnumValue(i, member, member.Type);
            }
        }

        private static EnumValue ReadEnumValue(int index, JToken member, JTokenType type)
        {
            switch (type)
            {
                case JTokenType.Object:
                    JProperty property = ((JObject)member).Properties().Single();
                    return ReadEnumValue(property.Name, (JValue)property.Value, property.Value.Type);

                case JTokenType.String:
                    return EnumValue.ImplicitValue((string)((JValue)member).Value, index);

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private static EnumValue ReadEnumValue(string name, JValue value, JTokenType type)
        {
            switch (type)
            {
                case JTokenType.Integer: return EnumValue.ExplicitValue(name, value.Value<int>());
                case JTokenType.String: return EnumValue.DynamicValue(name, (string)value.Value);
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private TypeReference ParsePropertyType(string typeName, string rootNamespace, string filePath, JToken value)
        {
            bool isEnumerable = typeName.EndsWith("*", StringComparison.Ordinal);
            typeName = typeName.TrimEnd('*');
            
            bool isNullable = typeName.EndsWith("?", StringComparison.Ordinal);
            typeName = typeName.TrimEnd('?');

            bool isTypeReference = typeName.StartsWith("#", StringComparison.Ordinal);
            typeName = typeName.TrimStart('#');

            if (isTypeReference)
            {
                IJsonLineInfo location = value.GetLineInfo();
                int column = location.LinePosition + 1;
                string key = $"{rootNamespace}.{typeName}";

                // Delay property contract reference tracking, since it might not be registered yet
                this._delayedCollectionActions.Add(new ContractReferenceCountCollectionAction(key, this._contracts));

                return new SchemaTypeReference(key, filePath, location.LineNumber, column, isNullable, isEnumerable);
            }

            PrimitiveType dataType = (PrimitiveType)Enum.Parse(typeof(PrimitiveType), typeName, ignoreCase: true /* JSON is camelCase while C# is PascalCase */);
            return new PrimitiveTypeReference(dataType, isNullable, isEnumerable);
        }
        #endregion

        #region Nested types
        private sealed class EnumValue
        {
            public string Name { get; }
            public int? ActualValue { get; }
            public string StringValue { get; }

            private EnumValue(string name, int? actualValue, string stringValue)
            {
                this.Name = name;
                this.ActualValue = actualValue;
                this.StringValue = stringValue;
            }

            public static EnumValue ImplicitValue(string name, int actualValue) => new EnumValue(name, actualValue, null);
            public static EnumValue ExplicitValue(string name, int actualValue) => new EnumValue(name, actualValue, actualValue.ToString(CultureInfo.InvariantCulture));
            public static EnumValue DynamicValue(string name, string stringValue) => new EnumValue(name, default, stringValue);
        }
        #endregion
    }
}