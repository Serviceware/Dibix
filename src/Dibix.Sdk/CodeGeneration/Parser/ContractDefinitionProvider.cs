using System;
using System.Collections.Generic;
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
        private readonly string _rootNamespace;
        private readonly string _productName;
        private readonly string _areaName;
        private readonly IDictionary<string, ContractDefinition> _contracts;
        private readonly ICollection<string> _referencedContracts;
        #endregion

        #region Properties
        public IEnumerable<ContractDefinition> Contracts => this._contracts.Values;
        protected override string SchemaName => "dibix.contracts.schema";
        IEnumerable<SchemaDefinition> ISchemaProvider.Schemas => this.Contracts.Select(x => x.Schema);
        #endregion

        #region Constructor
        public ContractDefinitionProvider(IFileSystemProvider fileSystemProvider, ILogger logger, IEnumerable<string> contracts, string rootNamespace, string productName, string areaName) : base(fileSystemProvider, logger)
        {
            this._rootNamespace = rootNamespace;
            this._productName = productName;
            this._areaName = areaName;
            this._contracts = new Dictionary<string, ContractDefinition>();
            this._referencedContracts = new HashSet<string>();
            this.CollectCore(contracts);
        }
        #endregion

        #region ISchemaProvider Members
        public bool TryGetSchema(string name, out SchemaDefinition schema)
        {
            if (!this._contracts.TryGetValue(name, out ContractDefinition contract))
            {
                schema = null;
                return false;
            }

            contract.HasReferences = true;
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
            string currentNamespace = NamespaceUtility.BuildAbsoluteNamespace(this._rootNamespace, this._productName, this._areaName, LayerName.DomainModel, relativeNamespace);

            string root = multipleAreas ? parts[1] : null;
            string rootNamespace = NamespaceUtility.BuildAbsoluteNamespace(this._rootNamespace, this._productName, this._areaName, LayerName.DomainModel, root);

            this.ReadContracts(rootNamespace, currentNamespace, json, filePath);
        }
        #endregion

        #region Private Methods
        private void CollectCore(IEnumerable<string> contracts)
        {
            base.Collect(contracts);
            this.CollectReferenceCounts();
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
                    JValue defaultValueJson = default;
                    SerializationBehavior serializationBehavior = default;
                    DateTimeKind dateTimeKind = default;
                    bool isPartOfKey = default;
                    bool isOptional = default;
                    bool isDiscriminator = default;
                    bool isObfuscated = default;
                    bool isRelativeHttpsUrl = default;
                    switch (property.Value.Type)
                    {
                        case JTokenType.Object:
                            JObject propertyInfo = (JObject)property.Value;
                            typeNameValue = (JValue)propertyInfo.Property("type").Value;
                            typeName = (string)typeNameValue;
                            defaultValueJson = (JValue)propertyInfo.Property("default")?.Value;
                            Enum.TryParse((string)propertyInfo.Property("serialize")?.Value, true, out serializationBehavior);
                            Enum.TryParse((string)propertyInfo.Property("kind")?.Value, true, out dateTimeKind);
                            isPartOfKey = (bool?)propertyInfo.Property("isPartOfKey")?.Value ?? default;
                            isOptional = (bool?)propertyInfo.Property("isOptional")?.Value ?? default;
                            isDiscriminator = (bool?)propertyInfo.Property("isDiscriminator")?.Value ?? default;
                            isObfuscated = (bool?)propertyInfo.Property("obfuscated")?.Value ?? default;
                            isRelativeHttpsUrl = (bool?)propertyInfo.Property("isRelativeHttpsUrl")?.Value ?? default;
                            break;

                        case JTokenType.String:
                            typeNameValue = (JValue)property.Value;
                            typeName = (string)typeNameValue;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(property.Type), property.Type, null);
                    }

                    TypeReference type = this.ParsePropertyType(typeName, rootNamespace, filePath, typeNameValue);
                    ValueReference defaultValue = null;
                    if (defaultValueJson != null)
                    {
                        IJsonLineInfo defaultValueLocation = defaultValueJson.GetLineInfo();
                        defaultValue = JsonValueReferenceParser.Parse(type, defaultValueJson, filePath, defaultValueLocation, Logger);
                    }

                    ObjectSchemaProperty objectSchemaProperty = new ObjectSchemaProperty(property.Name, type, defaultValue, serializationBehavior, dateTimeKind, isPartOfKey, isOptional, isDiscriminator, isObfuscated, isRelativeHttpsUrl);
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

            IJsonLineInfo location = value.GetLineInfo();
            if (isTypeReference)
            {
                // TODO: Actually the contracts should have a namespace group, just like the procedures.
                // The folder itself should not indicate a namespace (just like in C#).
                // Therefore a namespace property should be added to the contract schema.
                // This would however introduce a breaking change, since in C#, when you are within a namespace group,
                // and you want to reference a type from outside of the namespace, an absolute namespace must be used.
                string key = $"{rootNamespace}.{typeName}";
                this._referencedContracts.Add(key);
                return new SchemaTypeReference(key, isNullable, isEnumerable, filePath, location.LineNumber, location.LinePosition);
            }

            PrimitiveType dataType = (PrimitiveType)Enum.Parse(typeof(PrimitiveType), typeName, ignoreCase: true /* JSON is camelCase while C# is PascalCase */);
            return new PrimitiveTypeReference(dataType, isNullable, isEnumerable, filePath, location.LineNumber, location.LinePosition);
        }

        private void CollectReferenceCounts()
        {
            foreach (string name in this._referencedContracts)
            {
                if (!this._contracts.TryGetValue(name, out ContractDefinition contractDefinition))
                {
                    // The contract is quite possibly not defined in this project
                    continue;
                }
                contractDefinition.HasReferences = true;
            }
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