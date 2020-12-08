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
        private readonly string _productName;
        private readonly string _areaName;
        private readonly IDictionary<string, SchemaDefinition> _schemas;
        #endregion

        #region Properties
        public IEnumerable<SchemaDefinition> Contracts => this._schemas.Values;
        protected override string SchemaName => "dibix.contracts.schema";
        IEnumerable<SchemaDefinition> ISchemaProvider.Schemas => this.Contracts;
        #endregion

        #region Constructor
        public ContractDefinitionProvider(IFileSystemProvider fileSystemProvider, ILogger logger, IEnumerable<string> contracts, string productName, string areaName) : base(fileSystemProvider, logger)
        {
            this._productName = productName;
            this._areaName = areaName;
            this._schemas = new Dictionary<string, SchemaDefinition>();
            base.Collect(contracts);
        }
        #endregion

        #region ISchemaProvider Members
        public bool TryGetSchema(string name, out SchemaDefinition schema)
        {
            if (this._schemas.TryGetValue(name, out schema))
                return true;

            // Assume relative namespace
            string absoluteNamespace = NamespaceUtility.BuildAbsoluteNamespace(this._productName, this._areaName, LayerName.DomainModel, name);
            return this._schemas.TryGetValue(absoluteNamespace, out schema);
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
        private void ReadContracts(string rootNamespace, string currentNamespace, JObject contracts, string filePath)
        {
            foreach (JProperty definitionProperty in contracts.Properties())
            {
                this.ReadContract(rootNamespace, currentNamespace, definitionProperty.Name, definitionProperty.Value, filePath);
            }
        }

        private void ReadContract(string rootNamespace, string currentNamespace, string definitionName, JToken value, string filePath)
        {
            switch (value.Type)
            {
                case JTokenType.Object:
                    this.ReadObjectContract(rootNamespace, currentNamespace, definitionName, value, filePath);
                    break;

                case JTokenType.Array:
                    this.ReadEnumContract(currentNamespace, definitionName, value);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(value.Type), value.Type, null);
            }
        }

        private void ReadObjectContract(string rootNamespace, string currentNamespace, string definitionName, JToken value, string filePath)
        {
            ObjectSchema contract = new ObjectSchema(currentNamespace, definitionName);
            foreach (JProperty property in ((JObject)value).Properties())
            {
                if (property.Name == "$wcfNs")
                    contract.WcfNamespace = (string)property.Value;
                else
                {
                    JValue typeNameValue;
                    string typeName;
                    bool isPartOfKey = default;
                    bool isDiscriminator = default;
                    SerializationBehavior serializationBehavior = default;
                    DateTimeKind dateTimeKind = default;
                    bool obfuscated = default;
                    switch (property.Value.Type)
                    {
                        case JTokenType.Object:
                            JObject propertyInfo = (JObject)property.Value;
                            typeNameValue = (JValue)propertyInfo.Property("type").Value;
                            typeName = (string)typeNameValue;
                            isPartOfKey = (bool?)propertyInfo.Property("isPartOfKey")?.Value ?? default;
                            isDiscriminator = (bool?)propertyInfo.Property("isDiscriminator")?.Value ?? default;
                            Enum.TryParse((string)propertyInfo.Property("serialize")?.Value, true, out serializationBehavior);
                            if (String.Equals(typeName, nameof(PrimitiveDataType.DateTime), StringComparison.OrdinalIgnoreCase))
                                Enum.TryParse((string)propertyInfo.Property("kind")?.Value, true, out dateTimeKind);

                            obfuscated = (bool?)propertyInfo.Property("obfuscated")?.Value ?? default;
                            break;

                        case JTokenType.String:
                            typeNameValue = (JValue)property.Value;
                            typeName = (string)typeNameValue;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(property.Type), property.Type, null);
                    }

                    TypeReference type = ParseType(typeName, rootNamespace, filePath, typeNameValue);
                    contract.Properties.Add(new ObjectSchemaProperty(property.Name, type, isPartOfKey, isDiscriminator, serializationBehavior, dateTimeKind, obfuscated));
                }
            }

            this._schemas.Add(contract.FullName, contract);
        }

        private void ReadEnumContract(string currentNamespace, string definitionName, JToken definitionValue)
        {
            EnumSchema contract = new EnumSchema(currentNamespace, definitionName, false);

            ICollection<EnumValue> values = ReadEnumValues(definitionValue).ToArray();
            IDictionary<string, int> actualValues = values.Where(x => x.ActualValue.HasValue).ToDictionary(x => x.Name, x => x.ActualValue.Value);

            foreach (EnumValue value in values)
            {
                int actualValue = value.ActualValue ?? EnumValueParser.ParseDynamicValue(actualValues, value.StringValue);
                contract.Members.Add(new EnumSchemaMember(value.Name, actualValue, value.StringValue));
            }

            this._schemas.Add(contract.FullName, contract);
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

        private static TypeReference ParseType(string typeName, string rootNamespace, string filePath, JValue value)
        {
            bool isEnumerable = typeName.EndsWith("*", StringComparison.Ordinal);
            typeName = typeName.TrimEnd('*');
            
            bool isNullable = typeName.EndsWith("?", StringComparison.Ordinal);
            typeName = typeName.TrimEnd('?');

            bool isTypeReference = typeName.StartsWith("#", StringComparison.Ordinal);
            typeName = typeName.TrimStart('#');

            if (isTypeReference)
            {
                IJsonLineInfo location = value;
                int column = value.GetCorrectLinePosition() + 1;
                return new SchemaTypeReference($"{rootNamespace}.{typeName}", filePath, location.LineNumber, column, isNullable, isEnumerable);
            }

            PrimitiveDataType dataType = (PrimitiveDataType)Enum.Parse(typeof(PrimitiveDataType), typeName, true);
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